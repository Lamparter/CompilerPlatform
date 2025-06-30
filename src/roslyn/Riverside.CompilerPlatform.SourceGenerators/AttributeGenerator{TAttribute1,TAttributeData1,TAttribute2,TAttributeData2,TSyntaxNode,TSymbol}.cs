#if CSHARP // Visual Basic implementation is not available yet

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riverside.CompilerPlatform.SourceGenerators.Extensions;
using Riverside.Extensions.Accountability;
using System;
using System.Linq;
using System.Threading;

namespace Riverside.CompilerPlatform.SourceGenerators;

/// <summary>
/// Base generator for processing attribute-based code generation with two attribute types.
/// </summary>
/// <typeparam name="TAttribute1">The first attribute type to look for.</typeparam>
/// <typeparam name="TAttributeData1">The wrapper type for the first attribute data.</typeparam>
/// <typeparam name="TAttribute2">The second attribute type to look for.</typeparam>
/// <typeparam name="TAttributeData2">The wrapper type for the second attribute data.</typeparam>
/// <typeparam name="TSyntaxNode">The syntax node type to process.</typeparam>
/// <typeparam name="TSymbol">The symbol type to process.</typeparam>
[NotMyCode]
public abstract class AttributeGenerator<TAttribute1, TAttributeData1, TAttribute2, TAttributeData2, TSyntaxNode, TSymbol> : AttributeGenerator
	where TAttribute1 : Attribute
	where TAttributeData1 : struct
	where TAttribute2 : Attribute
	where TAttributeData2 : struct
	where TSyntaxNode : MemberDeclarationSyntax
	where TSymbol : ISymbol
{
	private readonly string _fullAttribute1Name = typeof(TAttribute1).FullName;
	private readonly string _fullAttribute2Name = typeof(TAttribute2).FullName;

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
		// Get the symbol for the node
		var uncastedSymbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
		if (uncastedSymbol is not TSymbol symbol)
			return;

		// Process attributes of the first type
		var attribute1s = (
			from attr in symbol.GetAttributes()
			where attr.AttributeClass?.ToDisplayString() == _fullAttribute1Name
			select TransformAttribute1(attr, compilation)
		).ToArray();

		// Process attributes of the second type
		var attribute2s = (
			from attr in symbol.GetAttributes()
			where attr.AttributeClass?.ToDisplayString() == _fullAttribute2Name
			select TransformAttribute2(attr, compilation)
		).ToArray();

		if (attribute1s.Length == 0 && attribute2s.Length == 0)
			return;

		// Generate output for the symbol
		string? generatedMemberCode = OnPointVisit(semanticModel, syntaxNode, symbol, attribute1s, attribute2s);
		if (generatedMemberCode == null)
			return;

		var containingClass = symbol.ContainingType;
		var genericParams = containingClass.TypeParameters;
		var classHeader = genericParams.Length == 0
			? containingClass.Name
			: $"{containingClass.Name}<{string.Join(", ", from x in genericParams select x.Name)}>";

		// Create the syntax tree using the shared method from the base class
		var syntaxTree = CreateLanguageSpecificSyntaxTree(
			symbol,
			containingClass.ContainingNamespace.ToDisplayString(),
			classHeader,
			generatedMemberCode);

		// Add the syntax tree to AdditionalSources
		if (_additionalSources != null)
			_additionalSources[GetGeneratedFileName(symbol.ToString())] = syntaxTree;
	}

	/// <summary>
	/// Transforms an attribute of the first type to its data representation.
	/// </summary>
	/// <param name="attributeData">The attribute data to transform.</param>
	/// <param name="compilation">The current compilation.</param>
	/// <returns>The transformed attribute data.</returns>
	protected abstract TAttributeData1 TransformAttribute1(AttributeData attributeData, Compilation compilation);

	/// <summary>
	/// Transforms an attribute of the second type to its data representation.
	/// </summary>
	/// <param name="attributeData">The attribute data to transform.</param>
	/// <param name="compilation">The current compilation.</param>
	/// <returns>The transformed attribute data.</returns>
	protected abstract TAttributeData2 TransformAttribute2(AttributeData attributeData, Compilation compilation);

	/// <summary>
	/// Called when visiting a symbol with attributes.
	/// </summary>
	/// <param name="semanticModel">The semantic model.</param>
	/// <param name="syntaxNode">The syntax node being processed.</param>
	/// <param name="symbol">The symbol being processed.</param>
	/// <param name="attribute1Data">The data for attributes of the first type.</param>
	/// <param name="attribute2Data">The data for attributes of the second type.</param>
	/// <returns>The generated code, or null if no code should be generated.</returns>
	protected abstract string? OnPointVisit(SemanticModel semanticModel, TSyntaxNode syntaxNode, TSymbol symbol, TAttributeData1[] attribute1Data, TAttributeData2[] attribute2Data);
}

#endif
