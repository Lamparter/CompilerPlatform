namespace Riverside.CompilerPlatform.SourceGenerators;

/// <summary>
/// Represents the result of processing a node.
/// </summary>
/// <param name="hasOutput">Whether the result has output.</param>
/// <param name="fileName">The file name for the output.</param>
/// <param name="content">The content for the output.</param>
internal sealed class GeneratorResult(bool hasOutput, string? fileName = null, string? content = null)
{
    /// <summary>
    /// Gets whether the result has output.
    /// </summary>
    public bool HasOutput { get; } = hasOutput;

    /// <summary>
    /// Gets the file name for the output.
    /// </summary>
    public string? FileName { get; } = fileName;

    /// <summary>
    /// Gets the content for the output.
    /// </summary>
    public string? Content { get; } = content;
}

