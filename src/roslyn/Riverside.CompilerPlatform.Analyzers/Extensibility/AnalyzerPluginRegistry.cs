using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Riverside.CompilerPlatform.Analyzers.Extensibility;

/// <summary>
/// Registry for analyzer plugins.
/// </summary>
public static class AnalyzerPluginRegistry
{
	private static readonly List<IAnalyzerPlugin> _plugins = new List<IAnalyzerPlugin>();

	/// <summary>
	/// Registers a plugin.
	/// </summary>
	public static void RegisterPlugin(IAnalyzerPlugin plugin)
	{
		_plugins.Add(plugin);
	}

	/// <summary>
	/// Gets all registered analyzers.
	/// </summary>
	public static IEnumerable<DiagnosticAnalyzer> GetAllAnalyzers()
	{
		return _plugins.SelectMany(p => p.GetAnalyzers());
	}

	/// <summary>
	/// Gets all registered code fix providers.
	/// </summary>
	public static IEnumerable<CodeFixProvider> GetAllCodeFixProviders()
	{
		return _plugins.SelectMany(p => p.GetCodeFixProviders());
	}
}