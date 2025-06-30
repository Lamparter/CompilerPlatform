using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Riverside.CompilerPlatform.Analyzers.Integration;

/// <summary>
/// Combines an analyzer and its corresponding fixer.
/// </summary>
public class AnalyzerWithFixer<TAnalyzer, TFixer>
	where TAnalyzer : DiagnosticAnalyzer, new()
	where TFixer : CodeFixProvider, new()
{
	/// <summary>
	/// Gets the analyzer.
	/// </summary>
	public TAnalyzer Analyzer { get; } = new TAnalyzer();

	/// <summary>
	/// Gets the fixer.
	/// </summary>
	public TFixer Fixer { get; } = new TFixer();

	/// <summary>
	/// Gets the diagnostic IDs that this analyzer/fixer handles.
	/// </summary>
	public ImmutableArray<string> DiagnosticIds
		=> Fixer.FixableDiagnosticIds;

	/// <summary>
	/// Gets the diagnostic descriptors from the analyzer.
	/// </summary>
	public ImmutableArray<DiagnosticDescriptor> DiagnosticDescriptors
		=> Analyzer.SupportedDiagnostics;
}
