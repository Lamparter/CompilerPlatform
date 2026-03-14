using Riverside.CompilerPlatform.SourceGenerators;

namespace Riverside.CompilerPlatform.Extensions;

/// <summary>
/// Provides extension methods for reporting diagnostics within source generators.
/// </summary>
public static class DiagnosticExtensions
{
	extension(Diagnostic diagnostic)
	{
		/// <summary>
		/// Reports a diagnostic to the source production context within the specified generator context.
		/// </summary>
		/// <param name="context">The generator context that provides access to the source production context for reporting diagnostics.</param>
		public void Report(GeneratorContext context)
		{
			context.SourceProductionContext.ReportDiagnostic(diagnostic);
		}
	}
}
