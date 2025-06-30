using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Riverside.CompilerPlatform.Analyzers.Extensibility;

/// <summary>
/// Represents a set of analyzer rules.
/// </summary>
public class RuleSet
{
	/// <summary>
	/// Gets the name of the rule set.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the diagnostic IDs in this rule set.
	/// </summary>
	public ImmutableArray<string> DiagnosticIds { get; }

	/// <summary>
	/// Creates a new rule set.
	/// </summary>
	public RuleSet(string name, IEnumerable<string> diagnosticIds)
	{
		Name = name;
		DiagnosticIds = diagnosticIds.ToImmutableArray();
	}

	/// <summary>
	/// Checks if a diagnostic ID is in this rule set.
	/// </summary>
	public bool Contains(string diagnosticId)
	{
		return DiagnosticIds.Contains(diagnosticId);
	}
}
