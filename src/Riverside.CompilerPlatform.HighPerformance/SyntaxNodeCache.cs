using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Riverside.CompilerPlatform.HighPerformance;

/// <summary>
/// Cache for commonly used syntax nodes.
/// </summary>
public static class SyntaxNodeCache
{
	private static readonly ConcurrentDictionary<string, SyntaxNode> _nodeCache =
		new ConcurrentDictionary<string, SyntaxNode>();

	/// <summary>
	/// Gets a cached syntax node created from source text.
	/// </summary>
	public static TNode GetOrCreate<TNode>(string sourceText)
		where TNode : SyntaxNode
	{
		string key = $"{typeof(TNode).Name}|{sourceText}";

		if (_nodeCache.TryGetValue(key, out var cachedNode) &&
			cachedNode is TNode typedNode)
		{
			return typedNode;
		}

		var node = SyntaxFactory.ParseSyntaxTree(sourceText)
			.GetRoot()
			.DescendantNodes()
			.OfType<TNode>()
			.First();

		_nodeCache[key] = node;
		return node;
	}

	/// <summary>
	/// Clears the cache.
	/// </summary>
	public static void Clear() => _nodeCache.Clear();
}
