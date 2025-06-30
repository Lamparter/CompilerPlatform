using System;

namespace Riverside.CompilerPlatform.Features.Iteration;

/// <summary>
/// Provides extension methods for performing repeated actions.
/// </summary>
/// <remarks>This class contains utility methods designed to simplify iteration tasks.</remarks>
public static class IterationHelper
{
	/// <summary>
	/// This method executes the specified action a given number of times.
	/// </summary>
	/// <param name="count">The amount of times to complete the action</param>
	/// <param name="action">The action to complete in the loop.</param>
	public static void Repeat(this int count, Action action)
	{
		for (int i = 0; i < count; i++) action();
	}
}