using Microsoft.CodeAnalysis;
using Riverside.Extensions.Accountability;
using System.Collections.Generic;

namespace Riverside.CompilerPlatform.SourceGenerators.Extensions;

/// <summary>
/// Provides extension methods for working with Roslyn symbols.
/// </summary>
[NotMyCode]
public static class SymbolExtensions
{
	private static readonly SymbolDisplayFormat FullSymbolDisplay = new(
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints | SymbolDisplayGenericsOptions.IncludeVariance,
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
	);

	/// <summary>
	/// Gets the full name of the type symbol, optionally including nullable reference type annotations.
	/// </summary>
	/// <param name="symbol">The type symbol.</param>
	/// <param name="nullableReferenceType">Indicates whether to include nullable reference type annotations.</param>
	/// <returns>The full name of the type symbol.</returns>
	public static string FullName(this ITypeSymbol symbol, bool nullableReferenceType = false)
	{
		nullableReferenceType = nullableReferenceType || symbol.NullableAnnotation == NullableAnnotation.Annotated;

		return nullableReferenceType && !symbol.IsValueType
			? symbol.FullNameWithoutAnnotation() + "?"
			: symbol.FullNameWithoutAnnotation();
	}

	/// <summary>
	/// Gets the full name of the type symbol without nullable annotations.
	/// </summary>
	/// <param name="symbol">The type symbol.</param>
	/// <returns>The full name of the type symbol without nullable annotations.</returns>
	public static string FullNameWithoutAnnotation(this ITypeSymbol symbol)
	{
		return symbol.ToDisplayString(FullSymbolDisplay);
	}

	/// <summary>
	/// Gets the escaped name of the symbol, ensuring it is prefixed with "@" if necessary.
	/// </summary>
	/// <param name="symbol">The symbol.</param>
	/// <returns>The escaped name of the symbol.</returns>
	public static string GetEscapedName(this ISymbol symbol)
	{
		return symbol.Name.StartsWith("@")
			? symbol.Name
			: $"@{symbol.Name}";
	}

	/// <summary>
	/// Recursively retrieves all members of the type, including those from its base types.
	/// </summary>
	/// <param name="type">The type symbol.</param>
	/// <returns>An enumerable of all members of the type and its base types.</returns>
	public static IEnumerable<ISymbol> GetMemberRecursiveBaseType(this ITypeSymbol? type)
	{
		if (type is null) yield break;

		foreach (var member in type.GetMembers())
			yield return member;

		foreach (var member in type.BaseType.GetMemberRecursiveBaseType())
			yield return member;
	}

	/// <summary>
	/// Determines whether the specified type is a subclass of the given base type.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <param name="baseType">The base type to compare against.</param>
	/// <returns><c>true</c> if the type is a subclass of the base type; otherwise, <c>false</c>.</returns>
	public static bool IsSubclassFrom(this INamedTypeSymbol? type, INamedTypeSymbol? baseType)
	{
		if (type == null || baseType == null)
			return false;

		if (type.Equals(baseType, SymbolEqualityComparer.Default))
			return true;

		return type.BaseType != null && type.BaseType.IsSubclassFrom(baseType);
	}

	/// <summary>
	/// Determines whether the specified type is the same as the given base type.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <param name="baseType">The base type to compare against.</param>
	/// <returns><c>true</c> if the type is the same as the base type; otherwise, <c>false</c>.</returns>
	public static bool IsTheSameAs(this INamedTypeSymbol? type, INamedTypeSymbol? baseType)
	{
		if (type == null || baseType == null)
			return false;

		return type.Equals(baseType, SymbolEqualityComparer.Default);
	}
}
