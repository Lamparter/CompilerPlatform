using Riverside.Extensions.Accountability;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Riverside.CompilerPlatform.Helpers;

/// <summary>
/// Provides utility methods for sanitising and escaping strings for safe use in file names and command-line arguments.
/// </summary>
public static class SanitizationHelpers
{
	/// <summary>
	/// Replaces invalid file name characters in the specified string with underscores to produce a safe file name.
	/// </summary>
	/// <param name="s">The input string to sanitise.</param>
	/// <returns>
	/// A string with all invalid file name characters replaced by underscores. The returned string is suitable for use as
	/// a file name.
	/// </returns>
	public static string Sanitize(string s)
	{
		var invalid = Path.GetInvalidFileNameChars().Append('-').ToArray();
		var sb = new StringBuilder(s.Length);
		foreach (var ch in s)
			sb.Append(invalid.Contains(ch) ? '_' : ch);
		return sb.ToString();
	}

	/// <summary>
	/// Escapes a command-line argument by quoting and handling special characters as required for safe parsing.
	/// </summary>
	/// <remarks>The returned string is quoted if it contains whitespace or special characters. Backslashes and
	/// quotes within the argument are escaped according to command-line parsing rules.</remarks>
	/// <param name="arg">The argument to escape.</param>
	/// <returns>A string containing the escaped argument, suitable for use in a command-line context.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="arg"/> is null.</exception>
	public static string EscapeArg(string arg)
	{
		if (arg == null)
			throw new ArgumentNullException(nameof(arg));

		if (arg.Length > 0 && arg.IndexOfAny(new[] { ' ', '\t', '\n', '\v', '"' }) == -1)
			return arg;

		var sb = new StringBuilder();
		sb.Append('"');

		int backslashCount = 0;
		foreach (char c in arg)
		{
			if (c == '\\')
			{
				backslashCount++;
			}
			else if (c == '"')
			{
				sb.Append('\\', backslashCount * 2 + 1);
				sb.Append('"');
				backslashCount = 0;
			}
			else
			{
				if (backslashCount > 0)
				{
					sb.Append('\\', backslashCount);
					backslashCount = 0;
				}
				sb.Append(c);
			}
		}

		if (backslashCount > 0)
			sb.Append('\\', backslashCount * 2);

		sb.Append('"');
		return sb.ToString();
	}
}
