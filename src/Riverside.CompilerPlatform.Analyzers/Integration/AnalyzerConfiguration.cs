using System;
using System.Collections.Generic;
using System.Text;

namespace Riverside.CompilerPlatform.Analyzers.Integration;

/// <summary>
/// Configuration options for analyzers.
/// </summary>
public class AnalyzerConfiguration
{
	/// <summary>
	/// Gets or sets whether to analyze generated code.
	/// </summary>
	public bool AnalyzeGeneratedCode { get; set; }

	/// <summary>
	/// Gets or sets whether to enable concurrent execution.
	/// </summary>
	public bool EnableConcurrentExecution { get; set; } = true;

	/// <summary>
	/// Gets or sets the severity level for diagnostics.
	/// </summary>
	public DiagnosticSeverity? DefaultSeverity { get; set; }

	/// <summary>
	/// Gets or sets custom message formats for diagnostics.
	/// </summary>
	public Dictionary<string, string> CustomMessageFormats { get; set; } =
		new Dictionary<string, string>();

	/// <summary>
	/// Applies the configuration to an analysis context.
	/// </summary>
	public void ApplyTo(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(
			AnalyzeGeneratedCode
				? GeneratedCodeAnalysisFlags.Analyze
				: GeneratedCodeAnalysisFlags.None);

		if (EnableConcurrentExecution)
			context.EnableConcurrentExecution();
	}
}
