using Riverside.Extensions.Accountability;
using System;
using System.Collections.Generic;
using System.Text;

namespace Riverside.CompilerPlatform.SourceGenerators.Extensions;

/// <summary>
/// Provides extension methods for various types.
/// </summary>
[NotMyCode]
public static class TypeExtensions
{
	/// <summary>
	/// Generates all possible combinations of the elements in the given array.
	/// </summary>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="items">The array of items to generate combinations for.</param>
	/// <returns>An enumerable of arrays representing all combinations.</returns>
	public static IEnumerable<T[]> AllCombinations<T>(this T[] items)
	{
		yield return items;
		for (var i = 0; i < items.Length; i++)
			foreach (var enumerables in AllCombinations(System.Linq.Enumerable.ToArray(SkipAtIndex(items, i))))
			{
				yield return enumerables;
			}
	}

	/// <summary>
	/// Attempts to cast the given object to the specified type. If the cast fails, returns the provided default value.
	/// </summary>
	/// <typeparam name="T">The target type to cast to.</typeparam>
	/// <param name="value">The object to cast.</param>
	/// <param name="defaultValue">The default value to return if the cast fails.</param>
	/// <returns>The casted value if successful; otherwise, the default value.</returns>
	public static T CastOrDefault<T>(this object? value, T defaultValue)
	{
		if (value is T castedValue) return castedValue;
		else return defaultValue;
	}

	/// <summary>
	/// Skips the element at the specified index in the given array and returns the remaining elements.
	/// </summary>
	/// <typeparam name="T">The type of elements in the array.</typeparam>
	/// <param name="items">The array of items.</param>
	/// <param name="index">The index of the element to skip.</param>
	/// <returns>An enumerable of the remaining elements.</returns>
	public static IEnumerable<T> SkipAtIndex<T>(this T[] items, int index)
	{
		for (var i = 0; i < items.Length; i++)
		{
			if (i == index) continue;
			yield return items[i];
		}
	}

	/// <summary>
	/// Converts the given object to a syntax-friendly string representation.
	/// </summary>
	/// <param name="obj">The object to convert.</param>
	/// <returns>A string representation of the object suitable for syntax usage.</returns>
	public static string ToSyntaxString(this object obj)
	{
		if (obj is bool booleanValue)
			return booleanValue ? "true" : "false";
		else if (obj is string stringValue)
		{
			var sb = new StringBuilder();
			sb.Append('"');
			foreach (char c in stringValue)
			{
				switch (c)
				{
					case '\\': sb.Append("\\\\"); break;
					case '\"': sb.Append("\\\""); break;
					case '\n': sb.Append("\\n"); break;
					case '\r': sb.Append("\\r"); break;
					case '\t': sb.Append("\\t"); break;
					case '\0': sb.Append("\\0"); break;
					case '\a': sb.Append("\\a"); break;
					case '\b': sb.Append("\\b"); break;
					case '\f': sb.Append("\\f"); break;
					case '\v': sb.Append("\\v"); break;
					default: sb.Append(c); break;
				}
			}
			sb.Append('"');
			return sb.ToString();
		}
		else if (obj.GetType().IsEnum)
		{
			return $"{obj.GetType().FullName}.{Enum.GetName(obj.GetType(), obj)}";
		}

		return obj.ToString();
	}
}
