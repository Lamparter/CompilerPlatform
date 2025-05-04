using Riverside.Extensions.Accountability;
using System;

namespace Riverside.CompilerPlatform.SourceGenerators.Extensions;

/// <summary>
/// Provides extension methods for string manipulation.
/// </summary>
[NotMyCode]
public static class StringExtensions
{
	/// <summary>
	/// Indents each line of the given string by a specified number of spaces and times.
	/// </summary>
	/// <param name="original">The original string to indent.</param>
	/// <param name="indentSpaces">The number of spaces to use for a single indentation level. Default is 4.</param>
	/// <param name="indentTimes">The number of indentation levels to apply. Default is 1.</param>
	/// <returns>A new string with each line indented.</returns>
	public static string Indent(this string original, int indentSpaces = 4, int indentTimes = 1)
	{
		var indent = new string(' ', indentSpaces * indentTimes);
		var slashNindent = $"{Environment.NewLine}{indent}";
		return indent + original.Replace(Environment.NewLine, slashNindent);
	}

	/// <summary>
	/// Indents all lines of the given string except the first line by a specified number of spaces and times.
	/// </summary>
	/// <param name="original">The original string to indent.</param>
	/// <param name="indentSpaces">The number of spaces to use for a single indentation level. Default is 4.</param>
	/// <param name="indentTimes">The number of indentation levels to apply. Default is 1.</param>
	/// <returns>A new string with all lines except the first indented.</returns>
	public static string IndentWithoutFirstLine(this string original, int indentSpaces = 4, int indentTimes = 1)
	{
		var indent = new string(' ', indentSpaces * indentTimes);
		var slashNindent = $"{Environment.NewLine}{indent}";
		return original.Replace(Environment.NewLine, slashNindent);
	}
}
