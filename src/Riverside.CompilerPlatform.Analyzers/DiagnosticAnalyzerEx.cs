using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Riverside.CompilerPlatform.Analyzers;

/// <summary>
/// Base class for diagnostic analyzers that simplifies syntax node analysis.
/// </summary>
public abstract class DiagnosticAnalyzerEx : DiagnosticAnalyzer
{
	// Common analyzer properties
	public override abstract ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

	// Template method pattern for customization
	/// <summary>
	/// Analyzes a syntax node for diagnostics.
	/// </summary>
	/// <param name="context">The syntax node analysis context.</param>
	protected abstract void AnalyzeNode(SyntaxNodeAnalysisContext context);

	/// <summary>
	/// Gets the syntax kinds that this analyzer is interested in.
	/// </summary>
	protected abstract ImmutableArray<SyntaxKind> SyntaxKindsOfInterest { get; }

	// Common initialization logic
	/// <summary>
	/// Initializes the analyzer.
	/// </summary>
	/// <param name="context">The analysis context.</param>
	public sealed override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		// Register only for syntax nodes the derived analyzer is interested in
		context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKindsOfInterest);
	}

	// Helper methods for common analyzer patterns
	/// <summary>
	/// Creates a diagnostic at the location of the specified syntax node.
	/// </summary>
	/// <param name="descriptor">The diagnostic descriptor.</param>
	/// <param name="node">The syntax node where the diagnostic should be reported.</param>
	/// <param name="messageArgs">The message arguments.</param>
	/// <returns>A diagnostic instance.</returns>
	protected static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor,
		SyntaxNode node, params object[] messageArgs)
	{
		return Diagnostic.Create(descriptor, node.GetLocation(), messageArgs);
	}

	/// <summary>
	/// Creates a diagnostic for a specific source span.
	/// </summary>
	/// <param name="descriptor">The diagnostic descriptor.</param>
	/// <param name="node">The syntax node containing the span.</param>
	/// <param name="span">The text span within the node where the diagnostic applies.</param>
	/// <param name="messageArgs">The message arguments.</param>
	/// <returns>A diagnostic instance.</returns>
	protected static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor,
		SyntaxNode node, TextSpan span, params object[] messageArgs)
	{
		var location = Location.Create(node.SyntaxTree, span);
		return Diagnostic.Create(descriptor, location, messageArgs);
	}

	/// <summary>
	/// Creates a diagnostic for a property location.
	/// </summary>
	/// <param name="descriptor">The diagnostic descriptor.</param>
	/// <param name="node">The syntax node containing the property.</param>
	/// <param name="propertyName">The name of the property to locate.</param>
	/// <param name="messageArgs">The message arguments.</param>
	/// <returns>A diagnostic instance.</returns>
	protected static Diagnostic CreatePropertyDiagnostic(DiagnosticDescriptor descriptor,
		SyntaxNode node, string propertyName, params object[] messageArgs)
	{
		// This can be implemented based on the specific language (C# or VB)
		// For a proper implementation, we'd need language-specific code
		return CreateDiagnostic(descriptor, node, messageArgs);
	}
}
