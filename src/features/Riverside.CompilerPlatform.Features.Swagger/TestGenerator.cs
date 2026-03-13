using System.Threading;

namespace Riverside.CompilerPlatform.SourceGenerators;

[Generator]
public class Example : IncrementalGenerator
{
	protected override void OnBeforeGeneration(GeneratorContext context, CancellationToken cancellationToken)
	{
		AddSource("test", "// generating in onbeforegeneration!!!\npublic class TESTING { }");
	}
}
