#if CSHARP // This API is not available in Visual Basic

using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace Riverside.CompilerPlatform.CodeFixers;

/// <summary>
/// Base class for code fixers that focus on attributes.
/// </summary>
public abstract class AttributeFixer : CodeFixer
{
	/// <summary>
	/// Finds the attribute syntax at the diagnostic location.
	/// </summary>
	protected AttributeSyntax? FindAttributeNode(SyntaxNode root, TextSpan diagnosticSpan)
	{
		return FindTargetNode<AttributeSyntax>(root, diagnosticSpan);
	}

	/// <summary>
	/// Finds the attribute list syntax at the diagnostic location.
	/// </summary>
	protected AttributeListSyntax? FindAttributeListNode(SyntaxNode root, TextSpan diagnosticSpan)
	{
		return FindTargetNode<AttributeListSyntax>(root, diagnosticSpan);
	}

	/// <summary>
	/// Creates a new attribute with different arguments.
	/// </summary>
	protected static AttributeSyntax ReplaceAttributeArguments(
		AttributeSyntax attribute,
		IEnumerable<AttributeArgumentSyntax> newArguments)
	{
		// Get the correct SyntaxFactory based on the language
		var argumentList = attribute.ArgumentList;
		if (argumentList == null)
		{
			// Create a new argument list
			return attribute.WithArgumentList(
				SyntaxFactory.AttributeArgumentList(
					SyntaxFactory.SeparatedList(newArguments)));
		}

		// Replace existing arguments
		return attribute.WithArgumentList(
			argumentList.WithArguments(
				SyntaxFactory.SeparatedList(newArguments)));
	}
}

#endif
