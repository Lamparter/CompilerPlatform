namespace Riverside.CompilerPlatform.SourceGenerators;

/// <summary>
/// Provides contextual information for source generators, including the current compilation and analyser configuration options.
/// </summary>
/// <remarks>
/// This class is used to encapsulate real-time compiler information, used for adding source texts that rely on information about the compilation outside of the source generator constructor.
/// </remarks>
/// <param name="compilation">The current compilation representing the code being analysed and generated against.</param>
/// <param name="options">The analyser configuration options provider, supplying additional context and settings for the generator.</param>
public class GeneratorContext(Compilation compilation, AnalyzerConfigOptionsProvider options)
{
	public Compilation Compilation { get; } = compilation;
	public AnalyzerConfigOptionsProvider Options { get; } = options;
}
