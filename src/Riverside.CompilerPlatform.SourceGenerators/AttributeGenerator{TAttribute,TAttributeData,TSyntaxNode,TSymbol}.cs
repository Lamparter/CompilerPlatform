#if CSHARP // Visual Basic implementation is not available yet

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riverside.CompilerPlatform.SourceGenerators.Extensions;
using Riverside.Extensions.Accountability;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Riverside.CompilerPlatform.SourceGenerators;

/// <summary>
/// Base generator for processing attribute-based code generation with a single attribute type.
/// </summary>
/// <typeparam name="TAttribute">The attribute type to look for.</typeparam>
/// <typeparam name="TAttributeData">The wrapper type for attribute data.</typeparam>
/// <typeparam name="TSyntaxNode">The syntax node type to process.</typeparam>
/// <typeparam name="TSymbol">The symbol type to process.</typeparam>
[NotMyCode]
public abstract class AttributeGenerator<TAttribute, TAttributeData, TSyntaxNode, TSymbol> : AttributeGenerator
	where TAttribute : Attribute
	where TSyntaxNode : MemberDeclarationSyntax
	where TSymbol : ISymbol
{
	private readonly string _fullAttributeName = typeof(TAttribute).FullName;

	/// <summary>
	/// Gets or sets whether to count subclasses of the attribute type.
	/// </summary>
	protected virtual bool CountAttributeSubclass => true;

	/// <inheritdoc/>
	public override void ProcessCompilation(Compilation compilation, CancellationToken cancellationToken)
	{
		// Get all syntax trees in the compilation
		foreach (var syntaxTree in compilation.SyntaxTrees)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// Get the semantic model for this syntax tree
			var semanticModel = compilation.GetSemanticModel(syntaxTree);

			// Find all relevant syntax nodes
			var nodes = syntaxTree.GetRoot(cancellationToken)
				.DescendantNodesAndSelf()
				.OfType<TSyntaxNode>()
				.Where(node => node.AttributeLists.Count > 0);

			foreach (var syntaxNode in nodes)
			{
				cancellationToken.ThrowIfCancellationRequested();
				ProcessSyntaxNode(syntaxNode, semanticModel, compilation, cancellationToken);
			}
		}
	}

	/// <summary>
	/// Process an individual syntax node.
	/// </summary>
	private void ProcessSyntaxNode(TSyntaxNode syntaxNode, SemanticModel semanticModel, Compilation compilation, CancellationToken cancellationToken)
	{
		// Get symbols for the node
		var symbols = GetSymbols(semanticModel, syntaxNode, cancellationToken).ToArray();
		if (symbols.Length == 0)
			return;

		// Get the attribute class
		var attributeClass = compilation.GetTypeByMetadataName(_fullAttributeName);
		if (attributeClass == null)
			return;

		// Process attributes
		var attributes = (
			from attr in symbols[0].GetAttributes()
			where CountAttributeSubclass
				? attr.AttributeClass?.IsSubclassFrom(attributeClass) ?? false
				: attr.AttributeClass?.IsTheSameAs(attributeClass) ?? false
			let wrapper = TransformAttribute(attr, compilation)
			where wrapper != null
			select (Original: attr, Wrapper: wrapper)
		).ToArray();

		if (attributes.Length == 0)
			return;

		// Generate output for each symbol
		var outputs = new List<string>();
		foreach (var symbol in symbols)
		{
			string? output = OnPointVisit(semanticModel, syntaxNode, symbol, attributes);
			if (output != null)
				outputs.Add(output);
		}

		if (outputs.Count == 0)
			return;

		string joinedOutput = string.Join(Environment.NewLine + Environment.NewLine, outputs);

		// Generate the complete source file
		var containingClass = symbols[0] is INamedTypeSymbol nts ? nts : symbols[0].ContainingType;
		var genericParams = containingClass.TypeParameters;
		var classHeader = genericParams.Length == 0
			? containingClass.Name
			: $"{containingClass.Name}<{string.Join(", ", from x in genericParams select x.Name)}>";

		// Create the syntax tree using the shared method from the base class
		string fileName = GetGeneratedFileName($"{string.Join(" ", from x in symbols select x.ToString().Replace('<', '[').Replace('>', ']'))}");
		var syntaxTree = CreateLanguageSpecificSyntaxTree(
			symbols[0],
			containingClass.ContainingNamespace.ToDisplayString(),
			classHeader,
			joinedOutput);

		if (_additionalSources != null)
			_additionalSources[fileName] = syntaxTree;
	}

	/// <summary>
	/// Gets the symbols for a syntax node.
	/// </summary>
	protected virtual IEnumerable<TSymbol> GetSymbols(SemanticModel semanticModel, TSyntaxNode syntaxNode, CancellationToken cancellationToken)
	{
		if (semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken) is TSymbol symbol)
			yield return symbol;
	}

	/// <summary>
	/// Transforms an attribute to its data representation.
	/// </summary>
	/// <param name="attributeData">The attribute data to transform.</param>
	/// <param name="compilation">The current compilation.</param>
	/// <returns>The transformed attribute data.</returns>
	protected abstract TAttributeData? TransformAttribute(AttributeData attributeData, Compilation compilation);

	/// <summary>
	/// Called when visiting a symbol with attributes.
	/// </summary>
	/// <param name="semanticModel">The semantic model.</param>
	/// <param name="syntaxNode">The syntax node being processed.</param>
	/// <param name="symbol">The symbol being processed.</param>
	/// <param name="attributeData">The attribute data.</param>
	/// <returns>The generated code, or null if no code should be generated.</returns>
	protected abstract string? OnPointVisit(SemanticModel semanticModel, TSyntaxNode syntaxNode, TSymbol symbol, (AttributeData Original, TAttributeData Wrapper)[] attributeData);
}

#endif
