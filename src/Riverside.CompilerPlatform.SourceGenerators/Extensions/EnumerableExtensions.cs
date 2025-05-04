using Riverside.Extensions.Accountability;
using System;
using System.Collections.Generic;

namespace Riverside.CompilerPlatform.SourceGenerators.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="IEnumerable{T}"/>.
/// </summary>
[NotMyCode]
public static class EnumerableExtensions
{
	/// <summary>
	/// Enumerates the elements of the collection, returning each element along with its index.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collection.</typeparam>
	/// <param name="TEnumerable">The enumerable collection to enumerate.</param>
	/// <returns>An enumerable of tuples where each tuple contains the index and the corresponding element.</returns>
	public static IEnumerable<(uint Index, T Item)> Enumerate<T>(this IEnumerable<T> TEnumerable)
	{
		uint i = 0;
		foreach (var a in TEnumerable)
			yield return (i++, a);
	}

	/// <summary>
	/// Returns the first element of the collection, or a specified default value if the collection is empty.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collection.</typeparam>
	/// <param name="items">The enumerable collection to search.</param>
	/// <param name="defaultValue">The default value to return if the collection is empty.</param>
	/// <returns>The first element of the collection, or the specified default value if the collection is empty.</returns>
	public static T FirstOrDefault<T>(this IEnumerable<T> items, T defaultValue)
	{
		foreach (var item in items)
			return item;
		return defaultValue;
	}

	/// <summary>
	/// Concatenates the elements of a collection of strings, using a specified separator between each element.
	/// </summary>
	/// <param name="original">The collection of strings to join.</param>
	/// <param name="joinchar">The string to use as a separator.</param>
	/// <returns>A single string that consists of the elements in the collection separated by the specified separator.</returns>
	public static string Join(this IEnumerable<string> original, string joinchar)
		=> string.Join(joinchar, original);

	/// <summary>
	/// Concatenates the elements of a collection of strings, using a double newline as a separator.
	/// </summary>
	/// <param name="original">The collection of strings to join.</param>
	/// <returns>A single string that consists of the elements in the collection separated by double newlines.</returns>
	public static string JoinDoubleNewLine(this IEnumerable<string> original)
		=> string.Join($"{Environment.NewLine}{Environment.NewLine}", original);

	/// <summary>
	/// Concatenates the elements of a collection of strings, using a single newline as a separator.
	/// </summary>
	/// <param name="original">The collection of strings to join.</param>
	/// <returns>A single string that consists of the elements in the collection separated by single newlines.</returns>
	public static string JoinNewLine(this IEnumerable<string> original)
		=> string.Join(Environment.NewLine, original);
}
