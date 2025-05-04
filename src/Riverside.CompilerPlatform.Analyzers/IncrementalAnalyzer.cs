using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Riverside.CompilerPlatform.Analyzers;

/// <summary>
/// Base class for incremental analyzers that cache and reuse analysis results.
/// </summary>
public abstract class IncrementalAnalyzer : DiagnosticAnalyzer
{
	// Store state between analysis runs for incremental computation
	private readonly ConcurrentDictionary<SyntaxTree, AnalysisState> _analysisState =
		new ConcurrentDictionary<SyntaxTree, AnalysisState>();

	// Override the base class property (removing the 'abstract' modifier)
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetSupportedDiagnostics();

	/// <summary>
	/// Gets the diagnostic descriptors supported by this analyzer.
	/// </summary>
	/// <returns>The supported diagnostic descriptors.</returns>
	protected abstract ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnostics();

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		// Only analyze syntax trees that have changed
		context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);

		// Clean up removed trees
		context.RegisterCompilationStartAction(compilationContext =>
		{
			compilationContext.RegisterCompilationEndAction(CleanupRemovedTrees);
		});
	}

	private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
	{
		var tree = context.Tree;

		// Check if we've seen this tree before
		var previousState = _analysisState.TryGetValue(tree, out var state)
			? state : new AnalysisState();

		// Check if the tree has changed by comparing content hashes
		if (tree.TryGetText(out var text))
		{
			var currentHash = ComputeHash(text);

			if (currentHash.SequenceEqual(previousState.ContentHash))
			{
				// Tree hasn't changed, reuse previous diagnostics
				foreach (var diagnostic in previousState.Diagnostics)
				{
					context.ReportDiagnostic(diagnostic);
				}
				return;
			}

			// Tree has changed, analyze it
			var diagnostics = new List<Diagnostic>();
			AnalyzeTreeIncremental(context, tree, diagnostics);

			// Store the results for future runs
			_analysisState[tree] = new AnalysisState(currentHash, diagnostics);
		}
		else
		{
			// Couldn't get text, analyze anyway
			var diagnostics = new List<Diagnostic>();
			AnalyzeTreeIncremental(context, tree, diagnostics);

			// Store without hash
			_analysisState[tree] = new AnalysisState(diagnostics: diagnostics);
		}
	}

	/// <summary>
	/// Computes a simple hash of the source text for change detection.
	/// </summary>
	private byte[] ComputeHash(SourceText text)
	{
		// Use the text content to compute a hash
		using var sha = System.Security.Cryptography.SHA256.Create();
		return sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
	}

	/// <summary>
	/// Analyzes a syntax tree incrementally.
	/// </summary>
	/// <param name="context">The syntax tree analysis context.</param>
	/// <param name="tree">The syntax tree to analyze.</param>
	/// <param name="diagnostics">The list to add diagnostics to.</param>
	protected abstract void AnalyzeTreeIncremental(
		SyntaxTreeAnalysisContext context,
		SyntaxTree tree,
		List<Diagnostic> diagnostics);

	private void CleanupRemovedTrees(CompilationAnalysisContext context)
	{
		// Remove state for trees that are no longer in the compilation
		var currentTrees = new HashSet<SyntaxTree>(context.Compilation.SyntaxTrees);
		foreach (var tree in _analysisState.Keys.ToArray())
		{
			if (!currentTrees.Contains(tree))
			{
				_analysisState.TryRemove(tree, out _);
			}
		}
	}

	/// <summary>
	/// Represents the analysis state for a syntax tree.
	/// </summary>
	private class AnalysisState
	{
		public AnalysisState(byte[]? contentHash = null, IEnumerable<Diagnostic>? diagnostics = null)
		{
			ContentHash = contentHash ?? Array.Empty<byte>();
			Diagnostics = diagnostics?.ToImmutableArray() ?? ImmutableArray<Diagnostic>.Empty;
		}

		/// <summary>
		/// Gets the content hash of the syntax tree.
		/// </summary>
		public byte[] ContentHash { get; }

		/// <summary>
		/// Gets the diagnostics produced for the syntax tree.
		/// </summary>
		public ImmutableArray<Diagnostic> Diagnostics { get; }
	}
}
