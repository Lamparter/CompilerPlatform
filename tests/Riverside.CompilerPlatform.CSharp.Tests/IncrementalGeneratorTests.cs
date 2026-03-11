using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Riverside.CompilerPlatform.SourceGenerators;
using System.Reflection;

namespace Riverside.CompilerPlatform.CSharp.Tests;

[TestClass]
public sealed class IncrementalGeneratorTests
{
	[TestMethod]
	public async Task GeneratesSourceFromAddSourceMethod()
	{
		// Arrange
		var generator = new TestSourceGenerator();
		var compilation = CreateCompilation("public class TestClass {}");

		// Act
		var result = RunGenerator(compilation, generator);

		// Assert
		Assert.AreEqual(1, result.GeneratedTrees.Length);
		Assert.IsTrue(result.GeneratedTrees[0].FilePath.Contains("Generated_1.g.cs"));
		Assert.IsTrue(result.GeneratedTrees[0].GetText().ToString().Contains("public class GeneratedClass"));
	}

	[TestMethod]
	public async Task UsesCustomFileNames()
	{
		// Arrange
		var generator = new TestSourceGeneratorWithCustomNames();
		var compilation = CreateCompilation("public class TestClass {}");

		// Act
		var result = RunGenerator(compilation, generator);

		// Assert
		Assert.AreEqual(1, result.GeneratedTrees.Length);
		Assert.IsTrue(result.GeneratedTrees[0].FilePath.Contains("CustomName.g.cs"));
	}

	[TestMethod]
	public async Task HandlesMultipleSourceFiles()
	{
		// Arrange
		var generator = new TestSourceGeneratorWithMultipleFiles();
		var compilation = CreateCompilation("public class TestClass {}");

		// Act
		var result = RunGenerator(compilation, generator);

		// Assert
		Assert.AreEqual(2, result.GeneratedTrees.Length, "Should generate exactly 2 source files");

		// Use Path.GetFileName to handle full paths and check if file names contain expected patterns
		var fileNames = result.GeneratedTrees.Select(s => s.FilePath).ToArray();

		// Verify that we have files matching our expected naming pattern
		Assert.IsTrue(fileNames.Any(f => f.Contains("GeneratedFile1.g.cs")),
					 "First generated file name not found");

		Assert.IsTrue(fileNames.Any(f => f.Contains("GeneratedFile2.g.cs")),
					 "Second generated file name not found");

		// Additional verification - check content to ensure proper order
		var firstFile = result.GeneratedTrees.FirstOrDefault(t =>
			t.GetText().ToString().Contains("GeneratedClass1"));
		var secondFile = result.GeneratedTrees.FirstOrDefault(t =>
			t.GetText().ToString().Contains("GeneratedClass2"));

		Assert.IsNotNull(firstFile, "First class content not found");
		Assert.IsNotNull(secondFile, "Second class content not found");
	}

	[TestMethod]
	public async Task ReportsExpectedDiagnostics()
	{
		// Arrange
		var generator = new TestSourceGeneratorWithDiagnostics();
		var compilation = CreateCompilation("public class TestClass {}");

		// Act
		var result = RunGenerator(compilation, generator);

		// Assert
		Assert.AreEqual(1, result.Diagnostics.Length);
		Assert.AreEqual("TEST001", result.Diagnostics[0].Id);
		Assert.AreEqual("Test Diagnostic", result.Diagnostics[0].GetMessage());
	}

	[TestMethod]
	public async Task SupportsMultipleSourcesViaAddSource()
	{
		// Arrange
		var generator = new TestSourceGeneratorWithMultipleSources();
		var compilation = CreateCompilation("public class TestClass {}");

		// Act
		var result = RunGenerator(compilation, generator);

		// Assert
		Assert.AreEqual(2, result.GeneratedTrees.Length);

		// Find the additional source
		var additionalSource = result.GeneratedTrees.FirstOrDefault(s =>
			Path.GetFileName(s.FilePath) == "AdditionalFile.g.cs" ||
			s.FilePath.Contains("AdditionalFile.g.cs"));

		Assert.IsNotNull(additionalSource, "Additional source file not found");
		Assert.IsTrue(additionalSource.GetText().ToString().Contains("public class AdditionalClass"));
	}

	[TestMethod]
	public async Task LifecycleMethodsAreCalled()
	{
		// Arrange
		var generator = new TestSourceGeneratorWithLifecycleTracking();
		var compilation = CreateCompilation("public class TestClass {}");

		// Act
		var result = RunGenerator(compilation, generator);

		// Assert
		Assert.IsTrue(generator.BeforeGenerationCalled);
		Assert.IsTrue(generator.AfterGenerationCalled);
	}

	[TestMethod]
	public async Task ContextIsAvailableInLifecycleMethods()
	{
		// Arrange
		var generator = new TestSourceGeneratorWithContextAccess();
		var compilation = CreateCompilation("public class TestClass {}");

		// Act
		var result = RunGenerator(compilation, generator);

		// Assert
		Assert.IsTrue(generator.ContextWasAvailable);
		Assert.IsNotNull(generator.CapturedCompilation);
		Assert.AreEqual("compilation", generator.CapturedCompilation.AssemblyName);
	}

	[TestMethod]
	public async Task GetGeneratedFileNameCreatesCorrectExtension()
	{
		// Arrange
		var fileName = IncrementalGenerator.GetGeneratedFileName("MyFile");

		// Assert
		Assert.AreEqual("MyFile.g.cs", fileName);
	}

	#region Helper Methods and Test Generators

	private static Compilation CreateCompilation(string source)
	{
		return CSharpCompilation.Create("compilation",
			new[] { CSharpSyntaxTree.ParseText(source) },
			new[] { MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location) },
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	private static GeneratorDriverRunResult RunGenerator(Compilation compilation, IIncrementalGenerator generator)
	{
		var driver = CSharpGeneratorDriver.Create(generator);
		driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
		return driver.GetRunResult();
	}

	// Test generator implementations
	private class TestSourceGenerator : IncrementalGenerator
	{
		protected override void OnBeforeGeneration(GeneratorContext context, CancellationToken cancellationToken)
		{
			AddSource(GetGeneratedFileName("Generated_1"), "public class GeneratedClass {}");
		}
	}

	private class TestSourceGeneratorWithCustomNames : IncrementalGenerator
	{
		protected override void OnBeforeGeneration(GeneratorContext context, CancellationToken cancellationToken)
		{
			AddSource("CustomName.g.cs", "public class GeneratedClass {}");
		}
	}

	private class TestSourceGeneratorWithMultipleFiles : IncrementalGenerator
	{
		protected override void OnBeforeGeneration(GeneratorContext context, CancellationToken cancellationToken)
		{
			AddSource("GeneratedFile1.g.cs", "public class GeneratedClass1 {}");
			AddSource("GeneratedFile2.g.cs", "public class GeneratedClass2 {}");
		}
	}

	private class TestSourceGeneratorWithDiagnostics : IncrementalGenerator
	{
		protected override void OnBeforeGeneration(GeneratorContext context, CancellationToken cancellationToken)
		{
			AddSource(GetGeneratedFileName("Generated_1"), "public class GeneratedClass {}");
		}

		protected override void OnAfterGeneration(SourceProductionContext context)
		{
			var diagnostic = Diagnostic.Create(
				new DiagnosticDescriptor(
					"TEST001",
					"Test Diagnostic Title",
					"Test Diagnostic",
					"Testing",
					DiagnosticSeverity.Warning,
					isEnabledByDefault: true),
				Location.None);

			context.ReportDiagnostic(diagnostic);
		}
	}

	private class TestSourceGeneratorWithMultipleSources : IncrementalGenerator
	{
		protected override void OnBeforeGeneration(GeneratorContext context, CancellationToken cancellationToken)
		{
			AddSource(GetGeneratedFileName("Generated_1"), "public class GeneratedClass {}");
			AddSource("AdditionalFile.g.cs", "public class AdditionalClass {}");
		}
	}

	private class TestSourceGeneratorWithLifecycleTracking : IncrementalGenerator
	{
		public bool BeforeGenerationCalled { get; private set; }
		public bool AfterGenerationCalled { get; private set; }

		protected override void OnBeforeGeneration(GeneratorContext context, CancellationToken cancellationToken)
		{
			BeforeGenerationCalled = true;
			AddSource(GetGeneratedFileName("Generated_1"), "public class GeneratedClass {}");
		}

		protected override void OnAfterGeneration(SourceProductionContext context)
		{
			AfterGenerationCalled = true;
		}
	}

	private class TestSourceGeneratorWithContextAccess : IncrementalGenerator
	{
		public bool ContextWasAvailable { get; private set; }
		public Compilation CapturedCompilation { get; private set; }

		protected override void OnBeforeGeneration(GeneratorContext context, CancellationToken cancellationToken)
		{
			ContextWasAvailable = context != null;
			CapturedCompilation = context?.Compilation;
			AddSource(GetGeneratedFileName("Generated_1"), "public class GeneratedClass {}");
		}
	}

	#endregion
}