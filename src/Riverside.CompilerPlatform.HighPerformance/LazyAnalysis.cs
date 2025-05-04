using System;
using System.Collections.Generic;
using System.Text;

namespace Riverside.CompilerPlatform.HighPerformance;

/// <summary>
/// Helper for lazy evaluation of syntax analysis.
/// </summary>
public static class LazyAnalysis
{
	/// <summary>
	/// Evaluates an analyzer function only if a condition is met.
	/// </summary>
	public static void AnalyzeIf(
		SyntaxNodeAnalysisContext context,
		Func<bool> condition,
		Action<SyntaxNodeAnalysisContext> analyze)
	{
		if (condition())
			analyze(context);
	}

	/// <summary>
	/// Evaluates an analyzer function only for nodes that match a predicate.
	/// </summary>
	public static void AnalyzeWhen<TNode>(
		SyntaxNodeAnalysisContext context,
		Func<TNode, bool> predicate,
		Action<SyntaxNodeAnalysisContext, TNode> analyze)
		where TNode : SyntaxNode
	{
		if (context.Node is TNode node && predicate(node))
			analyze(context, node);
	}
}
