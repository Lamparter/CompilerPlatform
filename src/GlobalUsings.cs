global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.Diagnostics;
global using Microsoft.CodeAnalysis.CodeActions;
global using Microsoft.CodeAnalysis.CodeFixes;
global using Microsoft.CodeAnalysis.Text;

#if CSHARP
global using Microsoft.CodeAnalysis.CSharp;
global using Microsoft.CodeAnalysis.CSharp.Syntax;
#elif VISUALBASIC
global using Microsoft.CodeAnalysis.VisualBasic;
global using Microsoft.CodeAnalysis.VisualBasic.Syntax;
#endif
