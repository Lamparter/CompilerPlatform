using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Riverside.CompilerPlatform.Analyzers;

/// <summary>
/// Base class for analyzers that focus on a specific attribute type.
/// </summary>
/// <typeparam name="TAttribute">The attribute type to analyze.</typeparam>
public abstract class AttributeAnalyzer<TAttribute> : DiagnosticAnalyzerEx
	where TAttribute : Attribute
{
	private readonly string _attributeTypeName = typeof(TAttribute).FullName;
	private readonly string _attributeShortName = typeof(TAttribute).Name;

	/// <summary>
	/// Gets or sets whether to also analyze subclasses of the target attribute.
	/// </summary>
	protected virtual bool IncludeAttributeSubclasses => false;

	/// <summary>
	/// Gets the set of syntax kinds to register for.
	/// By default, registers for attributes, but can be overridden.
	/// </summary>
	protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest =>
		ImmutableArray.Create(SyntaxKind.Attribute);

	/// <summary>
	/// Handles syntax node analysis by filtering for attributes of the target type.
	/// </summary>
	/// <param name="context">The syntax node analysis context.</param>
	protected sealed override void AnalyzeNode(SyntaxNodeAnalysisContext context)
	{
		// Skip generated code
		if (context.IsGeneratedCode)
			return;

		if (context.Node is AttributeSyntax attributeSyntax)
		{
			var semanticModel = context.SemanticModel;
			var attributeType = semanticModel.GetTypeInfo(attributeSyntax).Type;

			// Check if the attribute is of the target type or a subclass
			bool isTargetAttribute = false;

			if (attributeType != null)
			{
				string typeName = attributeType.ToDisplayString();

				isTargetAttribute = typeName == _attributeTypeName ||
					(IncludeAttributeSubclasses && IsSubclassOfAttribute(attributeType));
			}
			else
			{
				// Try to match by name if type info is not available
				var name = attributeSyntax.Name.ToString();
				isTargetAttribute = name.EndsWith(_attributeShortName) ||
					name.EndsWith(_attributeShortName + "Attribute");
			}

			if (isTargetAttribute)
			{
				AnalyzeAttribute(context, attributeSyntax);
			}
		}
	}

	/// <summary>
	/// Checks if a type is a subclass of the target attribute.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type is a subclass of the target attribute.</returns>
	private bool IsSubclassOfAttribute(ITypeSymbol type)
	{
		var currentType = type;
		while (currentType != null)
		{
			if (currentType.ToDisplayString() == _attributeTypeName)
				return true;

			currentType = currentType.BaseType;
		}

		return false;
	}

	/// <summary>
	/// Analyzes an attribute of the target type.
	/// </summary>
	/// <param name="context">The syntax node analysis context.</param>
	/// <param name="attributeSyntax">The attribute syntax node.</param>
	protected abstract void AnalyzeAttribute(
		SyntaxNodeAnalysisContext context,
		AttributeSyntax attributeSyntax);

	/// <summary>
	/// Gets the attribute target symbol (the element the attribute is applied to).
	/// </summary>
	/// <param name="context">The syntax node analysis context.</param>
	/// <param name="attributeSyntax">The attribute syntax.</param>
	/// <returns>The symbol that the attribute is applied to, or null if not found.</returns>
	protected ISymbol? GetAttributeTarget(SyntaxNodeAnalysisContext context, AttributeSyntax attributeSyntax)
	{
		// Navigate to the element that this attribute is applied to
		var attributeList = attributeSyntax.Parent as AttributeListSyntax;
		if (attributeList == null)
			return null;

		var target = attributeList.Parent;
		if (target == null)
			return null;

		return context.SemanticModel.GetDeclaredSymbol(target);
	}

	/// <summary>
	/// Gets the attribute data for this attribute instance.
	/// </summary>
	/// <param name="context">The syntax node analysis context.</param>
	/// <param name="attributeSyntax">The attribute syntax.</param>
	/// <returns>The attribute data, or null if not found.</returns>
	protected AttributeData? GetAttributeData(SyntaxNodeAnalysisContext context, AttributeSyntax attributeSyntax)
	{
		var target = GetAttributeTarget(context, attributeSyntax);
		if (target == null)
			return null;

		// Find the attribute data that matches this attribute syntax
		return target.GetAttributes()
			.FirstOrDefault(attr =>
				attr.ApplicationSyntaxReference?.GetSyntax() == attributeSyntax);
	}
}
