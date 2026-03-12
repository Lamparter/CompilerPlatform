using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Riverside.CompilerPlatform.SourceGenerators;

/// <summary>
/// An incremental generator that simplifies source generation while providing full access to <see cref="IIncrementalGenerator"/> capabilities.
/// </summary>
/// <remarks>
/// This class abstracts over the Roslyn <see cref="IIncrementalGenerator"/> APIs, allowing derived classes to simply provide generated code without dealing with pipeline complexity, while still providing access to advanced features when needed.
/// </remarks>
public abstract class IncrementalGenerator : IIncrementalGenerator, IDebuggableGenerator
{
	#region Properties
	/// <summary>
	/// Gets the collection of source texts, indexed by their unique names.
	/// </summary>
	/// <remarks>
	/// Each entry in the dictionary represents a named <see cref="SourceText"/>.
	/// To add new sources, use the <see cref="AddSource(string?, SourceText)"/> method.
	/// </remarks>
	public Dictionary<string, SourceText> Sources { get; internal set; } = [];

	/// <summary>
	/// Gets or sets the generator context associated with the current operation.
	/// </summary>
	/// <remarks>
	/// The <see cref="Context"/> instance is only instantiated right before source generation, so sources that rely on it must be generated via <see cref="OnBeforeGeneration(GeneratorContext, CancellationToken)"/> where <see cref="Context"/> is guaranteed.
	/// </remarks>
	public GeneratorContext? Context { get; internal set; }

	/// <summary>
	/// Gets a value indicating whether to suppress diagnostics from being reported during source generation.
	/// </summary>
	protected virtual bool SuppressDiagnostics => false;

	/// <summary>
	/// Gets metadata references available to the generator.
	/// </summary>
	protected List<MetadataReference> MetadataReferences { get; } = new List<MetadataReference>();

	/// <inheritdoc/>
	public virtual bool IsDebuggerEnabled { get => Debugger.IsAttached; private set => Debug(value); }
	#endregion

	#region Events
	/// <summary>
	/// Called before source generation begins.
	/// A <see cref="GeneratorContext"/> instance is passed to this method; add new source texts here if they require real-time compilation data.
	/// </summary>
	/// <remarks>
	/// This method is called during the generation process, and any exceptions raised will be reported to the user.
	/// To customise this, disable <see cref="SuppressDiagnostics"/>.
	/// </remarks>
	/// <param name="context">The current generator context.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	protected virtual void OnBeforeGeneration(GeneratorContext context, CancellationToken cancellationToken) { }

	/// <summary>
	/// Called after source generation completes.
	/// </summary>
	/// <remarks>
	/// This method is called during the generation process, and any exceptions raised will be reported to the user.
	/// To customise this, disable <see cref="SuppressDiagnostics"/>.
	/// </remarks>
	/// <param name="context">The source production context.</param>
	protected virtual void OnAfterGeneration(SourceProductionContext context) { }

	/// <summary>
	/// Called when metadata references are available.
	/// </summary>
	/// <param name="references">The metadata references collection.</param>
	protected virtual void OnMetadataReferencesAvailable(IReadOnlyList<MetadataReference> references) { }
	#endregion

	/// <summary>
	/// Customises how a syntax tree is converted to source text.
	/// </summary>
	/// <param name="syntaxTree">The syntax tree to convert.</param>
	/// <returns>The source text representation.</returns>
	protected virtual string GetSourceText(SyntaxTree syntaxTree)
	{
		return syntaxTree?.ToString() ?? string.Empty;
	}

	#region Wrappers
	/// <summary>
	/// Provides an opportunity for derived classes to customize the initialization pipeline.
	/// </summary>
	/// <remarks>
	/// Override this method to add custom pipeline stages or transformations.
	/// This is called before the standard pipeline is set up, allowing for advanced customization.
	/// </remarks>
	/// <param name="context">The initialisation context.</param>
	protected virtual void CustomizeInitialization(IncrementalGeneratorInitializationContext context) { }

	/// <summary>
	/// Adds a source text to the collection.
	/// </summary>
	/// <remarks>
	/// If the provided hint name is null, empty, or consists only of whitespace, a unique file name is generated to ensure each source is uniquely identified.
	/// </remarks>
	/// <param name="hintName">The optional hint name to associate with the source text.</param>
	/// <param name="text">The source text to add.</param>
	public void AddSource(string? hintName, SourceText text)
	{
		if (string.IsNullOrWhiteSpace(hintName))
		{
			hintName = GetGeneratedFileName(Guid.NewGuid().ToString());
		}

		hintName =
			hintName!.EndsWith(".vb", StringComparison.OrdinalIgnoreCase) ||
			hintName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
				? hintName
				: GetGeneratedFileName(hintName);

		Sources.Add(hintName!, text);
	}

	/// <inheritdoc cref="AddSource(string?, SourceText)" />
	public void AddSource(string? hintName, string text)
	{
		AddSource(hintName, SourceText.From(text, encoding: Encoding.UTF8));
	}

	/// <inheritdoc />
	public void Debug(bool toAttach = true)
	{
		if (toAttach)
			Debugger.Launch();
	}

	// Internal implementation so other source generator implementations in this assembly can override the Initialize() method
	internal virtual void Initialize(IncrementalGeneratorInitializationContext context, out bool cancelling)
	{
		cancelling = false;
	}

	/// <summary>
	/// Initialises the source generator.
	/// </summary>
	/// <remarks>
	/// This method should not be overriden, instead, override <see cref="CustomizeInitialization(IncrementalGeneratorInitializationContext)"/> for advanced customization of the generator instance.
	/// </remarks>
	/// <param name="context">The initialisation context provided by the Roslyn compiler.</param>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		Initialize(context, out bool cancelling);
		if (cancelling)
			return;

		// Allow advanced customization first
		CustomizeInitialization(context);

		// Register for metadata references
		var metadataProvider = context.CompilationProvider.Select((compilation, _) => compilation.References.ToList());
		context.RegisterSourceOutput(metadataProvider, (ctx, references) =>
		{
			MetadataReferences.Clear();
			MetadataReferences.AddRange(references);
			OnMetadataReferencesAvailable(references);
		});

		// Set up analysis options provider
		IncrementalValueProvider<AnalyzerConfigOptionsProvider> optionsProvider = context.AnalyzerConfigOptionsProvider;

		// Register main source generation pipeline
		context.RegisterSourceOutput(
			context.CompilationProvider.Combine(optionsProvider),
			(sourceProductionContext, tuple) =>
			{
				Compilation compilation = tuple.Left;
				AnalyzerConfigOptionsProvider options = tuple.Right;

				Context = new(compilation, options);

				try
				{
					// Call the before generation hook
					OnBeforeGeneration(Context, sourceProductionContext.CancellationToken);

					// Add the source to the compilation
					foreach (var entry in Sources)
					{
						sourceProductionContext.AddSource(entry.Key, entry.Value);
					}

					// Call the after generation hook
					OnAfterGeneration(sourceProductionContext);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					if (!SuppressDiagnostics)
					{
						// Report exceptions that occur during generation
						sourceProductionContext.ReportDiagnostic(
							CreateDiagnostic(
								$"RX9999",
								"Source Generation Error",
								$"An error occurred during source generation: {ex.Message}",
								DiagnosticSeverity.Error));
					}
				}
			});
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Helper method for derived classes to create a diagnostic.
	/// </summary>
	/// <param name="id">The diagnostic ID.</param>
	/// <param name="title">The diagnostic title.</param>
	/// <param name="message">The diagnostic message.</param>
	/// <param name="severity">The diagnostic severity.</param>
	/// <param name="location">The location associated with the diagnostic.</param>
	/// <returns>A diagnostic instance.</returns>
	protected static Diagnostic CreateDiagnostic(
		string id,
		string title,
		string message,
		DiagnosticSeverity severity = DiagnosticSeverity.Warning,
		Location? location = null)
	{
		return Diagnostic.Create(
			new DiagnosticDescriptor(
				id,
				title,
				message,
				"SourceGeneration",
				severity,
				isEnabledByDefault: true),
			location ?? Location.None);
	}

	/// <summary>
	/// Helper method to create a syntax diagnostic with location information.
	/// </summary>
	/// <param name="node">The syntax node where the diagnostic occurred.</param>
	/// <param name="id">The diagnostic ID.</param>
	/// <param name="title">The diagnostic title.</param>
	/// <param name="message">The diagnostic message.</param>
	/// <param name="severity">The diagnostic severity.</param>
	/// <returns>A diagnostic instance with location information.</returns>
	protected static Diagnostic CreateSyntaxDiagnostic(
		SyntaxNode node,
		string id,
		string title,
		string message,
		DiagnosticSeverity severity = DiagnosticSeverity.Warning)
	{
		Location location = node?.GetLocation() ?? Location.None;
		return CreateDiagnostic(id, title, message, severity, location);
	}

	/// <summary>
	/// Helper method to create a source text from a string.
	/// </summary>
	/// <param name="source">The source code as a string.</param>
	/// <returns>A SourceText instance.</returns>
	protected static SourceText CreateSourceText(string source)
	{
		return SourceText.From(source ?? string.Empty);
	}

	/// <summary>
	/// Helper method to create a transformation for incremental values.
	/// </summary>
	/// <typeparam name="TIn">The input type.</typeparam>
	/// <typeparam name="TOut">The output type.</typeparam>
	/// <param name="provider">The input provider.</param>
	/// <param name="transform">The transform function.</param>
	/// <returns>A provider for the transformed values.</returns>
	protected static IncrementalValuesProvider<TOut> Transform<TIn, TOut>(
		IncrementalValuesProvider<TIn> provider,
		Func<TIn, CancellationToken, TOut> transform)
	{
		return provider.Select(transform);
	}

	/// <summary>
	/// Helper method to combine multiple incremental value providers.
	/// </summary>
	/// <typeparam name="T1">The type of the first provider.</typeparam>
	/// <typeparam name="T2">The type of the second provider.</typeparam>
	/// <param name="provider1">The first provider.</param>
	/// <param name="provider2">The second provider.</param>
	/// <returns>A combined provider.</returns>
	protected static IncrementalValueProvider<(T1, T2)> Combine<T1, T2>(
		IncrementalValueProvider<T1> provider1,
		IncrementalValueProvider<T2> provider2)
	{
		return provider1.Combine(provider2);
	}

	/// <summary>
	/// Helper method to create a generated file name.
	/// </summary>
	/// <remarks>
	/// The returned file name extension is changed based on the <c>Riverside.CompilerPlatform</c> assembly that is imported, either for Visual Basic or C#.
	/// </remarks>
	/// <param name="value">The file name that will have the <c>.g.cs</c> or <c>.g.vb</c> extension appended to it.</param>
	/// <returns></returns>
	public static string GetGeneratedFileName(string value)
	{
		return value + ".g." +
#if VISUALBASIC
		"vb"
#elif CSHARP
		"cs"
#endif
		;
	}
	#endregion
}
