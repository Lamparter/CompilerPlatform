using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace Riverside.CompilerPlatform.Analyzers;

/// <summary>
/// Base class for analyzers that focus on specific syntax node types.
/// </summary>
/// <typeparam name="TMember">The type of syntax node to analyze.</typeparam>
public abstract class MemberAnalyzer<TMember> : DiagnosticAnalyzerEx
	where TMember : SyntaxNode
{
	/// <summary>
	/// Gets the syntax kinds relevant for the specified member type.
	/// </summary>
	protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
		=> GetSyntaxKindsForType();

	/// <summary>
	/// Handles syntax node analysis by filtering for nodes of the target type.
	/// </summary>
	/// <param name="context">The syntax node analysis context.</param>
	protected sealed override void AnalyzeNode(SyntaxNodeAnalysisContext context)
	{
		// Skip generated code
		if (context.IsGeneratedCode)
			return;

		if (context.Node is TMember memberSyntax)
		{
			AnalyzeMember(context, memberSyntax);
		}
	}

	/// <summary>
	/// Analyzes a syntax node of the target type.
	/// </summary>
	/// <param name="context">The syntax node analysis context.</param>
	/// <param name="memberSyntax">The syntax node to analyze.</param>
	protected abstract void AnalyzeMember(
		SyntaxNodeAnalysisContext context,
		TMember memberSyntax);

	/// <summary>
	/// Gets the symbol for the analyzed member, if available.
	/// </summary>
	/// <param name="context">The syntax node analysis context.</param>
	/// <param name="memberSyntax">The member syntax.</param>
	/// <returns>The symbol for the member, or null if not available.</returns>
	protected ISymbol? GetSymbol(SyntaxNodeAnalysisContext context, TMember memberSyntax)
	{
		return context.SemanticModel.GetDeclaredSymbol(memberSyntax);
	}

	/// <summary>
	/// Determines the syntax kinds relevant for the specified member type.
	/// </summary>
	/// <returns>The relevant syntax kinds.</returns>
	private ImmutableArray<SyntaxKind> GetSyntaxKindsForType()
	{
		// Implementations for common member types
		Type memberType = typeof(TMember);

		// TODO: Analyze the type to determine appropriate syntax kinds
		// Need to define mappings for any other types of members

#if CSHARP
		if (memberType == typeof(ClassDeclarationSyntax))
			return ImmutableArray.Create(SyntaxKind.ClassDeclaration);

		if (memberType == typeof(MethodDeclarationSyntax))
			return ImmutableArray.Create(SyntaxKind.MethodDeclaration);

		if (memberType == typeof(PropertyDeclarationSyntax))
			return ImmutableArray.Create(SyntaxKind.PropertyDeclaration);

		if (memberType == typeof(FieldDeclarationSyntax))
			return ImmutableArray.Create(SyntaxKind.FieldDeclaration);

		if (memberType == typeof(EnumDeclarationSyntax))
			return ImmutableArray.Create(SyntaxKind.EnumDeclaration);

		if (memberType == typeof(StructDeclarationSyntax))
			return ImmutableArray.Create(SyntaxKind.StructDeclaration);

		if (memberType == typeof(InterfaceDeclarationSyntax))
			return ImmutableArray.Create(SyntaxKind.InterfaceDeclaration);

		if (memberType == typeof(ParameterSyntax))
			return ImmutableArray.Create(SyntaxKind.Parameter);

		if (memberType == typeof(AttributeSyntax))
			return ImmutableArray.Create(SyntaxKind.Attribute);
#endif

		// Default to an empty set, which derived classes can override
		return ImmutableArray<SyntaxKind>.Empty;
	}
}
