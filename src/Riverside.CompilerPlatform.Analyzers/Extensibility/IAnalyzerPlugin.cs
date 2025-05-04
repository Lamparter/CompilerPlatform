using System;
using System.Collections.Generic;
using System.Text;

namespace Riverside.CompilerPlatform.Analyzers.Extensibility;

/// <summary>
/// Interface for analyzer plugins.
/// </summary>
public interface IAnalyzerPlugin
{
	/// <summary>
	/// Gets the analyzers provided by this plugin.
	/// </summary>
	IEnumerable<DiagnosticAnalyzer> GetAnalyzers();

	/// <summary>
	/// Gets the code fix providers provided by this plugin.
	/// </summary>
	IEnumerable<CodeFixProvider> GetCodeFixProviders();
}