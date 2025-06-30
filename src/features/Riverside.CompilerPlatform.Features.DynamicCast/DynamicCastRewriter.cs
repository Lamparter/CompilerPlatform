using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Riverside.CompilerPlatform.Features.DynamicCast;

public class DynamicCastRewriter : CSharpSyntaxRewriter
{
	private readonly SemanticModel _semanticModel;

	public DynamicCastRewriter(SemanticModel semanticModel)
	{
		_semanticModel = semanticModel;
	}

	public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
	{
		var declaration = node.Parent as VariableDeclarationSyntax;
		if (declaration?.Type == null || node.Initializer == null)
			return base.VisitVariableDeclarator(node);

		var targetType = _semanticModel.GetTypeInfo(declaration.Type).ConvertedType;
		var sourceExpr = node.Initializer.Value;
		return ApplyCastIfNeeded(node, node.Initializer, sourceExpr, targetType);
	}

	public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
	{
		var targetType = _semanticModel.GetTypeInfo(node.Left).ConvertedType;
		var sourceExpr = node.Right;
		return ApplyCastIfNeeded(node, node.Right, sourceExpr, targetType);
	}

	public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
	{
		var containingSymbol = _semanticModel.GetEnclosingSymbol(node.SpanStart) as IMethodSymbol;
		if (containingSymbol == null || node.Expression == null)
			return base.VisitReturnStatement(node);

		var returnType = containingSymbol.ReturnType;
		var sourceExpr = node.Expression;
		return ApplyCastIfNeeded(node, node.Expression, sourceExpr, returnType);
	}

	public override SyntaxNode VisitArgument(ArgumentSyntax node)
	{
		var parent = node.Parent?.Parent as InvocationExpressionSyntax;
		if (parent == null)
			return base.VisitArgument(node);

		var symbol = _semanticModel.GetSymbolInfo(parent).Symbol as IMethodSymbol;
		if (symbol == null)
			return base.VisitArgument(node);

		var args = parent.ArgumentList.Arguments;
		var index = args.IndexOf(node);
		if (index < 0 || index >= symbol.Parameters.Length)
			return base.VisitArgument(node);

		var targetType = symbol.Parameters[index].Type;
		var sourceExpr = node.Expression;
		return ApplyCastIfNeeded(node, node.Expression, sourceExpr, targetType);
	}

	public override SyntaxNode VisitConditionalExpression(ConditionalExpressionSyntax node)
	{
		var targetType = _semanticModel.GetTypeInfo(node).ConvertedType;

		var whenTrue = ApplyCastIfNeeded(node.WhenTrue, node.WhenTrue, node.WhenTrue, targetType);
		var whenFalse = ApplyCastIfNeeded(node.WhenFalse, node.WhenFalse, node.WhenFalse, targetType);

		return node.WithWhenTrue((ExpressionSyntax)whenTrue).WithWhenFalse((ExpressionSyntax)whenFalse);
	}

	public override SyntaxNode VisitInitializerExpression(InitializerExpressionSyntax node)
	{
		var parentTypeInfo = _semanticModel.GetTypeInfo(node.Parent);
		var elementType = (parentTypeInfo.Type as INamedTypeSymbol)?.TypeArguments.FirstOrDefault();

		if (elementType == null)
			return base.VisitInitializerExpression(node);

		var newExpressions = node.Expressions.Select(expr =>
		{
			var exprType = _semanticModel.GetTypeInfo(expr).Type;
			return NeedsDynamicCast(expr, exprType, elementType)
				? BuildCastExpression(expr, elementType)
				: expr;
		});

		return node.WithExpressions(SyntaxFactory.SeparatedList(newExpressions));
	}

	private SyntaxNode ApplyCastIfNeeded(SyntaxNode originalNode, SyntaxNode subNode, ExpressionSyntax sourceExpr, ITypeSymbol targetType)
	{
		var sourceType = _semanticModel.GetTypeInfo(sourceExpr).Type;

		if (NeedsDynamicCast(sourceExpr, sourceType, targetType))
		{
			var casted = BuildCastExpression(sourceExpr, targetType)
				.WithTriviaFrom(sourceExpr);
			return originalNode.ReplaceNode(subNode, casted);
		}

		return originalNode;
	}

	private bool NeedsDynamicCast(ExpressionSyntax expr, ITypeSymbol from, ITypeSymbol to)
	{
		if (from == null || to == null || from.Equals(to, SymbolEqualityComparer.Default))
			return false;

		var conversion = _semanticModel.ClassifyConversion(expr, to);
		return !conversion.IsImplicit && conversion.IsExplicit && conversion.IsReference;
	}

	private static ExpressionSyntax BuildCastExpression(ExpressionSyntax expr, ITypeSymbol targetType)
	{
		return SyntaxFactory.CastExpression(
			SyntaxFactory.ParseTypeName(targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
			SyntaxFactory.CastExpression(
				SyntaxFactory.ParseTypeName("object"),
				expr.WithoutTrivia()
			)
		);
	}
}
