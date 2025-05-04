# `Riverside.CompilerPlatform`

`Riverside.CompilerPlatform` provides extensions to Roslyn that make building code analysis tools simple.

---

You can build powerful source generators, analyzers, and code fixers using the tools provided by the `Riverside.CompilerPlatform` library.
It provides a simple abstraction over Roslyn's powerful yet often overwhelming APIs and makes the creation of code analysis tools simple and straight-forward while still having the full potential and capability of a regular code analysis tool that invokes the Roslyn APIs directly.

`Riverside.CompilerPlatform` is comprised of three main libraries:

- `Riverside.CompilerPlatform.Analyzers`: Exposes helpers and classes for building diagnostic analyzers
- `Riverside.CompilerPlatform.CodeFixers`: Exposes abstract classes for building functional code fixes
- `Riverside.CompilerPlatform.HighPerformance`: Exposes static helpers for making fast and efficient code analysis tools
- `Riverside.CompilerPlatform.SourceGenerators`: Exposes abstract classes to inherit from for building source generator tools

All of these libraries contain extensions to their respective code analysis tool types, and enable the building of efficient and advanced source generators via the abstractions they expose.
