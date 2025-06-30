using Microsoft.CodeAnalysis;
using Riverside.Extensions.Accountability;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Riverside.CompilerPlatform.SourceGenerators.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="IncrementalValueProvider{T}"/>.
/// </summary>
[NotMyCode]
public static class IncrementalValueProviderExtensions
{
	/// <summary>
	/// Projects each value of the <see cref="IncrementalValueProvider{T}"/> into a new form.
	/// </summary>
	/// <typeparam name="TIn">The type of the input value.</typeparam>
	/// <typeparam name="TOut">The type of the output value.</typeparam>
	/// <param name="valueProvider">The incremental value provider to transform.</param>
	/// <param name="func">A transform function to apply to each value.</param>
	/// <returns>An <see cref="IncrementalValueProvider{T}"/> whose values are the result of invoking the transform function on each value of the source provider.</returns>
	public static IncrementalValueProvider<TOut> Select<TIn, TOut>(this IncrementalValueProvider<TIn> valueProvider, Func<TIn, TOut> func)
	{
		return valueProvider.Select((x, _) => func(x));
	}

	/// <summary>
	/// Projects each value of the <see cref="IncrementalValueProvider{T}"/> into an <see cref="IEnumerable{T}"/> and flattens the resulting sequences into one sequence.
	/// </summary>
	/// <typeparam name="TIn">The type of the input value.</typeparam>
	/// <typeparam name="TOut">The type of the output values.</typeparam>
	/// <param name="valueProvider">The incremental value provider to transform.</param>
	/// <param name="func">A transform function to apply to each value that returns an <see cref="IEnumerable{T}"/>.</param>
	/// <returns>An <see cref="IncrementalValuesProvider{T}"/> whose values are the result of flattening the sequences returned by the transform function.</returns>
	public static IncrementalValuesProvider<TOut> SelectMany<TIn, TOut>(this IncrementalValueProvider<TIn> valueProvider, Func<TIn, IEnumerable<TOut>> func)
	{
		return valueProvider.SelectMany((x, _) => func(x));
	}

	// public static IncrementalValueProvider<TIn> Where<TIn>(this IncrementalValueProvider<TIn> valueProvider, Func<TIn, bool> func)
	// {
	//     return valueProvider.Where(func);
	// }
}
