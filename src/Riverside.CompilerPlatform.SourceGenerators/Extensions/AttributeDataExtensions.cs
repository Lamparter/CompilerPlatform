using Microsoft.CodeAnalysis;
using Riverside.Extensions.Accountability;
using System;

namespace Riverside.CompilerPlatform.SourceGenerators.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="AttributeData"/>.
/// </summary>
[NotMyCode]
public static class AttributeDataExtensions
{
	// public static T GetConstructor<T>(this AttributeData attribute, int index, T? defaultValue = default)
	// {
	//     var attr = attribute.
	// }

	/// <summary>
	/// Retrieves the value of a named property from the attribute data.
	/// </summary>
	/// <typeparam name="T">The expected type of the property value.</typeparam>
	/// <param name="attribute">The attribute data to retrieve the property from.</param>
	/// <param name="attributeName">The name of the property to retrieve.</param>
	/// <param name="defaultValue">The default value to return if the property is not found.</param>
	/// <returns>The value of the named property if found; otherwise, the default value.</returns>
	/// <exception cref="ArgumentException">Thrown if duplicate properties with the same name are found.</exception>
	public static T GetProperty<T>(this AttributeData attribute, string attributeName, T defaultValue)
	{
		bool first = true;
		T result = defaultValue;
		foreach (var v in attribute.NamedArguments)
		{
			if (v.Key == attributeName)
			{
				if (!first)
					throw new ArgumentException("Duplicate Element!");

				first = false;
				result = (T)v.Value!.Value!;
			}
		}
		return result;
	}
}
