#if CSHARP // This API is not available in Visual Basic

using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Riverside.CompilerPlatform.CodeFixers;

/// <summary>
/// Base class for code fixers that focus on member declarations.
/// </summary>
public abstract class MemberFixer<TMember> : CodeFixer
	where TMember : MemberDeclarationSyntax
{
	/// <summary>
	/// Finds the member syntax at the diagnostic location.
	/// </summary>
	protected TMember? FindMemberNode(SyntaxNode root, TextSpan diagnosticSpan)
	{
		return FindTargetNode<TMember>(root, diagnosticSpan);
	}

	/// <summary>
	/// Applies common transformations to a member declaration.
	/// </summary>
	protected virtual TMember TransformMember(TMember member, Diagnostic diagnostic)
	{
		// Derived classes should override this with type-specific transformations
		return member;
	}

	/// <summary>
	/// Base implementation that handles finding and transforming the member.
	/// </summary>
	protected override async Task<Document> ApplyFix(
		Document document,
		SyntaxNode root,
		SyntaxNode node,
		Diagnostic diagnostic,
		CancellationToken cancellationToken)
	{
		var member = FindMemberNode(root, node.Span);
		if (member == null)
			return document;

		var newMember = TransformMember(member, diagnostic);
		return CreateChangedDocument(document, root, member, newMember);
	}
}

#endif
