using System;
using System.Linq;

namespace Riverside.CompilerPlatform.Extensions;

/// <summary>
/// Provides extension methods for reading typed values from <see cref="AnalyzerConfigOptions"/>.
/// </summary>
public static class AnalyzerConfigOptionsExtensions
{
	/// <summary>
	/// Returns the string value for <paramref name="key"/>, or <see langword="null"/> if the key is absent or whitespace.
	/// </summary>
	/// <param name="options">The analyser config options to read from.</param>
	/// <param name="key">The property key, typically prefixed with <c>build_property.</c>.</param>
	/// <returns>The trimmed string value, or <see langword="null"/>.</returns>
	public static string? GetString(this AnalyzerConfigOptions options, string key)
	{
		options.TryGetValue(key, out var value);
		return string.IsNullOrWhiteSpace(value) ? null : value;
	}

	/// <summary>
	/// Returns a nullable <see cref="bool"/> for <paramref name="key"/>.
	/// Returns <see langword="null"/> when the key is absent, empty, or not a valid boolean string.
	/// </summary>
	/// <param name="options">The analyzer config options to read from.</param>
	/// <param name="key">The property key.</param>
	/// <returns>The parsed boolean, or <see langword="null"/>.</returns>
	public static bool? GetNullableBool(this AnalyzerConfigOptions options, string key)
	{
		var value = options.GetString(key);
		return value is not null && bool.TryParse(value, out var result) ? result : null;
	}

	/// <summary>
	/// Returns a nullable <typeparamref name="TEnum"/> for <paramref name="key"/>, parsed case-insensitively.
	/// Returns <see langword="null"/> when the key is absent, empty, or does not map to a valid enum member.
	/// </summary>
	/// <typeparam name="TEnum">The enum type to parse into.</typeparam>
	/// <param name="options">The analyser config options to read from.</param>
	/// <param name="key">The property key.</param>
	/// <returns>The parsed enum value, or <see langword="null"/>.</returns>
	public static TEnum? GetNullableEnum<TEnum>(this AnalyzerConfigOptions options, string key)
		where TEnum : struct, Enum
	{
		var value = options.GetString(key);
		return value is not null && Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) ? result : null;
	}

	/// <summary>
	/// Returns a <see cref="string"/>[] by splitting <paramref name="key"/>'s value on the <c>|</c> character.
	/// Empty or whitespace-only segments are discarded.
	/// Returns <see langword="null"/> when the key is absent, empty, or yields no usable segments.
	/// </summary>
	/// <param name="options">The analyser config options to read from.</param>
	/// <param name="key">The property key.</param>
	/// <returns>A non-empty trimmed array of segments, or <see langword="null"/>.</returns>
	public static string[]? GetPipeSeparatedArray(this AnalyzerConfigOptions options, string key)
	{
		var value = options.GetString(key);
		if (value is null)
			return null;
		var parts = value.Split('|')
			.Select(p => p.Trim())
			.Where(p => p.Length > 0)
			.ToArray();
		return parts.Length > 0 ? parts : null;
	}

	/// <summary>
	/// Returns a <typeparamref name="TEnum"/>[] by splitting <paramref name="key"/>'s value on <c>|</c> and parsing each segment case-insensitively.
	/// Segments that do not match a valid enum member are silently skipped.
	/// </summary>
	/// <typeparam name="TEnum">The enum type to parse each segment into.</typeparam>
	/// <param name="options">The analyser config options to read from.</param>
	/// <param name="key">The property key.</param>
	/// <returns>A non-empty array of parsed enum values, or <see langword="null"/> when the <paramref name="key"/> is absent, empty, or yields no valid members.</returns>
	public static TEnum[]? GetPipeSeparatedEnumArray<TEnum>(this AnalyzerConfigOptions options, string key)
		where TEnum : struct, Enum
	{
		var raw = options.GetPipeSeparatedArray(key);
		if (raw is null)
			return null;
		var parsed = raw
			.Select(s => (ok: Enum.TryParse<TEnum>(s, ignoreCase: true, out var v), val: v))
			.Where(t => t.ok)
			.Select(t => t.val)
			.ToArray();
		return parsed.Length > 0 ? parsed : null;
	}
}
