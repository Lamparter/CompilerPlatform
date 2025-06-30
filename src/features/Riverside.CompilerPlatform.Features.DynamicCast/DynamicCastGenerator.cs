using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;

namespace Riverside.CompilerPlatform.Features.DynamicCast;

[Generator]
public sealed class DynamicCastGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var syntaxProvider = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: static (node, _) => node is CompilationUnitSyntax,
				transform: static (ctx, _) => ctx.SemanticModel.Compilation)
			.Where(static c => c is not null);

		context.RegisterSourceOutput(syntaxProvider, (spc, compilation) =>
		{
			foreach (var tree in compilation.SyntaxTrees)
			{
				var semanticModel = compilation.GetSemanticModel(tree);
				var rewriter = new DynamicCastRewriter(semanticModel);
				var newRoot = rewriter.Visit(tree.GetRoot());

				if (!newRoot.IsEquivalentTo(tree.GetRoot()))
				{
					var fileName = $"DynamicCast_{Path.GetFileNameWithoutExtension(tree.FilePath)}.g.cs";
					spc.AddSource(fileName, newRoot.NormalizeWhitespace().ToFullString());
				}
			}
		});
	}
}
