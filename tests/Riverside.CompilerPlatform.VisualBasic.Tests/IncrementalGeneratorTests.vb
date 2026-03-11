Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Riverside.CompilerPlatform.SourceGenerators

Namespace Riverside.CompilerPlatform.VisualBasic.Tests

    <TestClass>
    Public NotInheritable Class IncrementalGeneratorTests

        <TestMethod>
        Public Async Function GeneratesSourceFromAddSourceMethod() As Task
            ' Arrange
            Dim generator As New TestSourceGenerator()
            Dim compilation = CreateCompilation("Public Class TestClass {}")

            ' Act
            Dim result = RunGenerator(compilation, generator)

            ' Assert
            Assert.AreEqual(1, result.GeneratedTrees.Length)
            Assert.IsTrue(result.GeneratedTrees(0).FilePath.Contains("Generated_1.g.vb"))
            Assert.IsTrue(result.GeneratedTrees(0).GetText().ToString().Contains("Public Class GeneratedClass"))
        End Function

        <TestMethod>
        Public Async Function UsesCustomFileNames() As Task
            ' Arrange
            Dim generator As New TestSourceGeneratorWithCustomNames()
            Dim compilation = CreateCompilation("Public Class TestClass {}")

            ' Act
            Dim result = RunGenerator(compilation, generator)

            ' Assert
            Assert.AreEqual(1, result.GeneratedTrees.Length)
            Assert.IsTrue(result.GeneratedTrees(0).FilePath.Contains("CustomName.g.vb"))
        End Function

        <TestMethod>
        Public Async Function HandlesMultipleSourceFiles() As Task
            ' Arrange
            Dim generator As New TestSourceGeneratorWithMultipleFiles()
            Dim compilation = CreateCompilation("Public Class TestClass {}")

            ' Act
            Dim result = RunGenerator(compilation, generator)

            ' Assert
            Assert.AreEqual(2, result.GeneratedTrees.Length, "Should generate exactly 2 source files")

            Dim fileNames = result.GeneratedTrees.Select(Function(s) s.FilePath).ToArray()

            Assert.IsTrue(fileNames.Any(Function(f) f.Contains("GeneratedFile1.g.vb")), "First generated file name not found")
            Assert.IsTrue(fileNames.Any(Function(f) f.Contains("GeneratedFile2.g.vb")), "Second generated file name not found")

            Dim firstFile = result.GeneratedTrees.FirstOrDefault(Function(t) t.GetText().ToString().Contains("GeneratedClass1"))
            Dim secondFile = result.GeneratedTrees.FirstOrDefault(Function(t) t.GetText().ToString().Contains("GeneratedClass2"))

            Assert.IsNotNull(firstFile, "First class content not found")
            Assert.IsNotNull(secondFile, "Second class content not found")
        End Function

        <TestMethod>
        Public Async Function ReportsExpectedDiagnostics() As Task
            ' Arrange
            Dim generator As New TestSourceGeneratorWithDiagnostics()
            Dim compilation = CreateCompilation("Public Class TestClass {}")

            ' Act
            Dim result = RunGenerator(compilation, generator)

            ' Assert
            Assert.AreEqual(1, result.Diagnostics.Length)
            Assert.AreEqual("TEST001", result.Diagnostics(0).Id)
            Assert.AreEqual("Test Diagnostic", result.Diagnostics(0).GetMessage())
        End Function

        <TestMethod>
        Public Async Function SupportsMultipleSourcesViaAddSource() As Task
            ' Arrange
            Dim generator As New TestSourceGeneratorWithMultipleSources()
            Dim compilation = CreateCompilation("Public Class TestClass {}")

            ' Act
            Dim result = RunGenerator(compilation, generator)

            ' Assert
            Assert.AreEqual(2, result.GeneratedTrees.Length)

            Dim additionalSource = result.GeneratedTrees.FirstOrDefault(Function(s) IO.Path.GetFileName(s.FilePath) = "AdditionalFile.g.vb" OrElse s.FilePath.Contains("AdditionalFile.g.vb"))

            Assert.IsNotNull(additionalSource, "Additional source file not found")
            Assert.IsTrue(additionalSource.GetText().ToString().Contains("Public Class AdditionalClass"))
        End Function

        <TestMethod>
        Public Async Function LifecycleMethodsAreCalled() As Task
            ' Arrange
            Dim generator As New TestSourceGeneratorWithLifecycleTracking()
            Dim compilation = CreateCompilation("Public Class TestClass {}")

            ' Act
            Dim result = RunGenerator(compilation, generator)

            ' Assert
            Assert.IsTrue(generator.BeforeGenerationCalled)
            Assert.IsTrue(generator.AfterGenerationCalled)
        End Function

        <TestMethod>
        Public Async Function ContextIsAvailableInLifecycleMethods() As Task
            ' Arrange
            Dim generator As New TestSourceGeneratorWithContextAccess()
            Dim compilation = CreateCompilation("Public Class TestClass {}")

            ' Act
            Dim result = RunGenerator(compilation, generator)

            ' Assert
            Assert.IsTrue(generator.ContextWasAvailable)
            Assert.IsNotNull(generator.CapturedCompilation)
            Assert.AreEqual("compilation", generator.CapturedCompilation.AssemblyName)
        End Function

        <TestMethod>
        Public Async Function GetGeneratedFileNameCreatesCorrectExtension() As Task
            ' Arrange
            Dim fileName = IncrementalGenerator.GetGeneratedFileName("MyFile")

            ' Assert
            Assert.AreEqual("MyFile.g.vb", fileName)
        End Function

#Region "Helper Methods and Test Generators"

        Private Shared Function CreateCompilation(source As String) As Compilation
            Return VisualBasicCompilation.Create("compilation",
                {VisualBasicSyntaxTree.ParseText(source)},
                {MetadataReference.CreateFromFile(GetType(Object).GetTypeInfo().Assembly.Location)},
                New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
        End Function

        Private Shared Function RunGenerator(compilation As Compilation, generator As IIncrementalGenerator) As GeneratorDriverRunResult
            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator.AsSourceGenerator()))
            driver = DirectCast(driver.RunGeneratorsAndUpdateCompilation(compilation, Nothing, Nothing), VisualBasicGeneratorDriver)
            Return driver.GetRunResult()
        End Function

        ' Test generator implementations
        Private Class TestSourceGenerator
            Inherits IncrementalGenerator

            Protected Overrides Sub OnBeforeGeneration(context As GeneratorContext, cancellationToken As CancellationToken)
                AddSource(GetGeneratedFileName("Generated_1"), "Public Class GeneratedClass {}")
            End Sub
        End Class

        Private Class TestSourceGeneratorWithCustomNames
            Inherits IncrementalGenerator

            Protected Overrides Sub OnBeforeGeneration(context As GeneratorContext, cancellationToken As CancellationToken)
                AddSource("CustomName.g.vb", "Public Class GeneratedClass {}")
            End Sub
        End Class

        Private Class TestSourceGeneratorWithMultipleFiles
            Inherits IncrementalGenerator

            Protected Overrides Sub OnBeforeGeneration(context As GeneratorContext, cancellationToken As CancellationToken)
                AddSource("GeneratedFile1.g.vb", "Public Class GeneratedClass1 {}")
                AddSource("GeneratedFile2.g.vb", "Public Class GeneratedClass2 {}")
            End Sub
        End Class

        Private Class TestSourceGeneratorWithDiagnostics
            Inherits IncrementalGenerator

            Protected Overrides Sub OnBeforeGeneration(context As GeneratorContext, cancellationToken As CancellationToken)
                AddSource(GetGeneratedFileName("Generated_1"), "Public Class GeneratedClass {}")
            End Sub

            Protected Overrides Sub OnAfterGeneration(context As SourceProductionContext)
                Dim diag = Diagnostic.Create(
                    New DiagnosticDescriptor(
                        "TEST001",
                        "Test Diagnostic Title",
                        "Test Diagnostic",
                        "Testing",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault:=True),
                    Location.None)

                context.ReportDiagnostic(diag)
            End Sub
        End Class

        Private Class TestSourceGeneratorWithMultipleSources
            Inherits IncrementalGenerator

            Protected Overrides Sub OnBeforeGeneration(context As GeneratorContext, cancellationToken As CancellationToken)
                AddSource(GetGeneratedFileName("Generated_1"), "Public Class GeneratedClass {}")
                AddSource("AdditionalFile.g.vb", "Public Class AdditionalClass {}")
            End Sub
        End Class

        Private Class TestSourceGeneratorWithLifecycleTracking
            Inherits IncrementalGenerator

            Public Property BeforeGenerationCalled As Boolean
            Public Property AfterGenerationCalled As Boolean

            Protected Overrides Sub OnBeforeGeneration(context As GeneratorContext, cancellationToken As CancellationToken)
                BeforeGenerationCalled = True
                AddSource(GetGeneratedFileName("Generated_1"), "Public Class GeneratedClass {}")
            End Sub

            Protected Overrides Sub OnAfterGeneration(context As SourceProductionContext)
                AfterGenerationCalled = True
            End Sub
        End Class

        Private Class TestSourceGeneratorWithContextAccess
            Inherits IncrementalGenerator

            Public Property ContextWasAvailable As Boolean
            Public Property CapturedCompilation As Compilation

            Protected Overrides Sub OnBeforeGeneration(context As GeneratorContext, cancellationToken As CancellationToken)
                ContextWasAvailable = context IsNot Nothing
                CapturedCompilation = context?.Compilation
                AddSource(GetGeneratedFileName("Generated_1"), "Public Class GeneratedClass {}")
            End Sub
        End Class

#End Region

    End Class

End Namespace
