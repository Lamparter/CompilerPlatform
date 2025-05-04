using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Riverside.CompilerPlatform.CodeFixers;

/// <summary>
/// Base class for code fixers that simplifies implementation.
/// </summary>
public abstract class CodeFixer : CodeFixProvider
{
	/// <summary>
	/// Gets the diagnostic IDs that this code fixer can fix.
	/// </summary>
	public override abstract ImmutableArray<string> FixableDiagnosticIds { get; }

	/// <summary>
	/// Gets the fix all provider for this code fixer. Defaults to batch fixer.
	/// </summary>
	/// <returns>The fix all provider.</returns>
	public sealed override FixAllProvider GetFixAllProvider() =>
		WellKnownFixAllProviders.BatchFixer;

	/// <summary>
	/// Registers code fixes for the diagnostics.
	/// </summary>
	/// <param name="context">The code fix context.</param>
	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document
			.GetSyntaxRootAsync(context.CancellationToken)
			.ConfigureAwait(false);

		if (root == null)
			return;

		var diagnostic = context.Diagnostics.First();
		var diagnosticSpan = diagnostic.Location.SourceSpan;

		// Find the syntax node at the diagnostic location
		var node = root.FindNode(diagnosticSpan);

		// Register a code action that will invoke the fix
		context.RegisterCodeFix(
			CodeAction.Create(
				title: GetFixTitle(diagnostic),
				createChangedDocument: c => ApplyFix(context.Document, root, node, diagnostic, c),
				equivalenceKey: GetEquivalenceKey(diagnostic)),
			diagnostic);
	}

	/// <summary>
	/// Registers multiple possible fixes for a diagnostic.
	/// </summary>
	protected void RegisterMultipleFixes(
		CodeFixContext context,
		Diagnostic diagnostic,
		Document document,
		SyntaxNode root,
		SyntaxNode node,
		IEnumerable<(string title, Func<CancellationToken, Task<Document>> createFix, string? equivalenceKey)> fixes)
	{
		foreach (var (title, createFix, equivalenceKey) in fixes)
		{
			context.RegisterCodeFix(
				CodeAction.Create(
					title: title,
					createChangedDocument: createFix,
					equivalenceKey: equivalenceKey ?? $"{GetType().Name}|{diagnostic.Id}|{title}"),
				diagnostic);
		}
	}

	/// <summary>
	/// Gets the title for the code fix.
	/// </summary>
	/// <param name="diagnostic">The diagnostic being fixed.</param>
	/// <returns>The title to display for the code fix.</returns>
	protected virtual string GetFixTitle(Diagnostic diagnostic) => "Fix issue";

	/// <summary>
	/// Gets an equivalence key for the code fix.
	/// </summary>
	/// <param name="diagnostic">The diagnostic being fixed.</param>
	/// <returns>The equivalence key.</returns>
	protected virtual string GetEquivalenceKey(Diagnostic diagnostic) =>
		$"{GetType().Name}|{diagnostic.Id}";

	/// <summary>
	/// Applies the code fix to the document.
	/// </summary>
	/// <param name="document">The document to fix.</param>
	/// <param name="root">The syntax root of the document.</param>
	/// <param name="node">The syntax node at the diagnostic location.</param>
	/// <param name="diagnostic">The diagnostic being fixed.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The updated document with the fix applied.</returns>
	protected abstract Task<Document> ApplyFix(
		Document document,
		SyntaxNode root,
		SyntaxNode node,
		Diagnostic diagnostic,
		CancellationToken cancellationToken);

	/// <summary>
	/// Creates a new root with a replacement node.
	/// </summary>
	/// <param name="document">The document to update.</param>
	/// <param name="root">The existing syntax root.</param>
	/// <param name="oldNode">The node to replace.</param>
	/// <param name="newNode">The replacement node.</param>
	/// <returns>The updated document.</returns>
	protected static Document CreateChangedDocument(Document document, SyntaxNode root, SyntaxNode oldNode, SyntaxNode newNode)
	{
		var newRoot = root.ReplaceNode(oldNode, newNode);
		return document.WithSyntaxRoot(newRoot);
	}

	/// <summary>
	/// Allows selection of specific node types from the diagnostic location.
	/// </summary>
	/// <typeparam name="TNode">The type of syntax node to find.</typeparam>
	/// <param name="root">The syntax root.</param>
	/// <param name="diagnosticSpan">The diagnostic span.</param>
	/// <returns>The found node of the specified type, or null if not found.</returns>
	protected static TNode? FindTargetNode<TNode>(SyntaxNode root, TextSpan diagnosticSpan)
		where TNode : SyntaxNode
	{
		var node = root.FindNode(diagnosticSpan);

		// Try to get the exact node
		if (node is TNode targetNode)
			return targetNode;

		// Try parent nodes
		while (node != null)
		{
			node = node.Parent;
			if (node is TNode parentNode)
				return parentNode;
		}

		return null;
	}

	/// <summary>
	/// Creates a preview of what the fix will do.
	/// </summary>
	protected static async Task<string> CreatePreviewAsync(
		Document document,
		Func<SyntaxNode, SyntaxNode> transform,
		CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root == null)
			return string.Empty;

		var newRoot = transform(root);
		return newRoot.ToFullString();
	}
}
