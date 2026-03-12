namespace Riverside.CompilerPlatform.SourceGenerators;

/// <summary>
/// Represents a source generator that provides additional debugging capabilities during incremental code generation.
/// </summary>
public interface IDebuggableGenerator
{
	/// <summary>
	/// Gets a value indicating whether the debugger is enabled for the current context.
	/// </summary>
	bool IsDebuggerEnabled { get; }

	/// <summary>
	/// Initialises the debugger instance on the source generator.
	/// </summary>
	/// <remarks>
	/// You should call <see cref="Debug"/> inside your <see cref="IIncrementalGenerator"/>'s constructor.
	/// </remarks>
	/// <param name="toAttach">Whether or not to start the debugger instance.</param>
	void Debug(bool toAttach);
}
