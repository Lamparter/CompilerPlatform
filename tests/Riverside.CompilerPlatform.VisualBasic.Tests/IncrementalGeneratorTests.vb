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
        Public Async Function GeneratesSourceFromCodeProperty() As Task
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

            Assert.IsTrue(fileNames.Any(Function(f) f.Contains("GeneratedFile1.g.vb") OrElse f.Contains("Generated_1.g.vb")), "First generated file name not found")
            Assert.IsTrue(fileNames.Any(Function(f) f.Contains("GeneratedFile2.g.vb") OrElse f.Contains("Generated_2.g.vb")), "Second generated file name not found")

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
        Public Async Function SupportsAdditionalSources() As Task
            ' Arrange
            Dim generator As New TestSourceGeneratorWithAdditionalSources()
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

            Private _code As List(Of SyntaxTree)

            Public Overrides Property Code As List(Of SyntaxTree)
                Get
                    Return If(_code, New List(Of SyntaxTree) From {
                        VisualBasicSyntaxTree.ParseText("Public Class GeneratedClass {}")
                    })
                End Get
                Set(value As List(Of SyntaxTree))
                    _code = value
                End Set
            End Property
        End Class

        Private Class TestSourceGeneratorWithCustomNames
            Inherits IncrementalGenerator

            Private _code As List(Of SyntaxTree)

            Public Overrides Property Code As List(Of SyntaxTree)
                Get
                    Return If(_code, New List(Of SyntaxTree) From {
                        VisualBasicSyntaxTree.ParseText("Public Class GeneratedClass {}")
                    })
                End Get
                Set(value As List(Of SyntaxTree))
                    _code = value
                End Set
            End Property

            Protected Overrides ReadOnly Property FileNames As IList(Of String)
                Get
                    Return New List(Of String) From {"CustomName.g.vb"}
                End Get
            End Property
        End Class

        Private Class TestSourceGeneratorWithMultipleFiles
            Inherits IncrementalGenerator

            Private _code As List(Of SyntaxTree)

            Public Overrides Property Code As List(Of SyntaxTree)
                Get
                    Return If(_code, New List(Of SyntaxTree) From {
                        VisualBasicSyntaxTree.ParseText("Public Class GeneratedClass1 {}"),
                        VisualBasicSyntaxTree.ParseText("Public Class GeneratedClass2 {}")
                    })
                End Get
                Set(value As List(Of SyntaxTree))
                    _code = value
                End Set
            End Property

            Protected Overrides ReadOnly Property FileNames As IList(Of String)
                Get
                    Return New List(Of String) From {"Generated_1.g.vb", "Generated_2.g.vb"}
                End Get
            End Property
        End Class

        Private Class TestSourceGeneratorWithDiagnostics
            Inherits IncrementalGenerator

            Private _code As List(Of SyntaxTree)

            Public Overrides Property Code As List(Of SyntaxTree)
                Get
                    Return If(_code, New List(Of SyntaxTree) From {
                        VisualBasicSyntaxTree.ParseText("Public Class GeneratedClass {}")
                    })
                End Get
                Set(value As List(Of SyntaxTree))
                    _code = value
                End Set
            End Property

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

        Private Class TestSourceGeneratorWithAdditionalSources
            Inherits IncrementalGenerator

            Private _code As List(Of SyntaxTree)
            Private _additionalSources As Dictionary(Of String, SyntaxTree)

            Public Overrides Property Code As List(Of SyntaxTree)
                Get
                    Return If(_code, New List(Of SyntaxTree) From {
                        VisualBasicSyntaxTree.ParseText("Public Class GeneratedClass {}")
                    })
                End Get
                Set(value As List(Of SyntaxTree))
                    _code = value
                End Set
            End Property

            Protected Overrides ReadOnly Property AdditionalSources As Dictionary(Of String, SyntaxTree)
                Get
                    Return If(_additionalSources, New Dictionary(Of String, SyntaxTree) From {
                        {"AdditionalFile.g.vb", VisualBasicSyntaxTree.ParseText("Public Class AdditionalClass {}")}
                    })
                End Get
            End Property
        End Class

        Private Class TestSourceGeneratorWithLifecycleTracking
            Inherits IncrementalGenerator

            Private _code As List(Of SyntaxTree)

            Public Overrides Property Code As List(Of SyntaxTree)
                Get
                    Return If(_code, New List(Of SyntaxTree) From {
                        VisualBasicSyntaxTree.ParseText("Public Class GeneratedClass {}")
                    })
                End Get
                Set(value As List(Of SyntaxTree))
                    _code = value
                End Set
            End Property

            Public Property BeforeGenerationCalled As Boolean
            Public Property AfterGenerationCalled As Boolean

            Protected Overrides Sub OnBeforeGeneration(compilation As Compilation, cancellationToken As CancellationToken)
                BeforeGenerationCalled = True
            End Sub

            Protected Overrides Sub OnAfterGeneration(context As SourceProductionContext)
                AfterGenerationCalled = True
            End Sub
        End Class

#End Region

    End Class

End Namespace
