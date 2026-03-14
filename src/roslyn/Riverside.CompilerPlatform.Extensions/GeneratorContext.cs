using System.Collections.Immutable;

namespace Riverside.CompilerPlatform.SourceGenerators;

/// <summary>
/// Provides contextual information for source generators, including the current compilation and analyser configuration options.
/// </summary>
/// <remarks>
/// This class is used to encapsulate real-time compiler information, used for adding source texts that rely on information about the compilation outside of the source generator constructor.
/// </remarks>
/// <param name="compilation">The current compilation, representing the code being analysed and generated against.</param>
/// <param name="analyzerConfigOptions">The analyser configuration options provider, supplying additional context and settings for the generator.</param>
/// <param name="syntax">The syntax value provider, supplying information that allows the generator to create Syntax based input nodes.</param>
/// <param name="text">The additional texts incremental values provider, supplying additional files to the generator that would not be handled by the compiler.</param>
/// <param name="parseOptions">The parse options incremental value provider, representing the parse options available in C# and Visual Basic.</param>
public class GeneratorContext(AnalyzerConfigOptionsProvider analyzerConfigOptions, Compilation compilation, SyntaxValueProvider syntax, ImmutableArray<AdditionalText> text, ParseOptions parseOptions)
{
	internal SourceProductionContext SourceProductionContext { get; set; }

	/// <summary>
	/// Gets the set of analyser configuration options associated with this provider, e.g. the local <c>.editorconfig</c> file.
	/// </summary>
	public AnalyzerConfigOptionsProvider AnalyzerConfigOptions { get; } = analyzerConfigOptions;

	/// <summary>
	/// Gets the current compilation associated with this instance.
	/// </summary>
	public Compilation Compilation { get; } = compilation;

	/// <summary>
	/// Gets the provider used to access syntax information for the current context.
	/// </summary>
	public SyntaxValueProvider Syntax { get; } = syntax;

	/// <summary>
	/// Gets the provider that supplies additional text files to be included in the incremental processing pipeline.
	/// </summary>
	public ImmutableArray<AdditionalText> AdditionalTexts { get; } = text;

	/// <summary>
	/// Gets the provider that supplies incremental parse options for the project.
	/// </summary>
	public ParseOptions ParseOptions { get; } = parseOptions;
}
