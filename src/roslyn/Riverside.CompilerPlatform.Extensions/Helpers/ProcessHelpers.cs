using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.CompilerPlatform.Helpers;

/// <summary>
/// Provides helper methods for running external processes and capturing their output and exit codes.
/// </summary>
public static class ProcessHelpers
{
	/// <summary>
	/// Represents the result of a process execution, including the exit code and captured output streams.
	/// </summary>
	/// <param name="code">The exit code returned by the process.</param>
	/// <param name="stdout">The text output written to the standard output stream by the process.</param>
	/// <param name="stderr">The text output written to the standard error stream by the process.</param>
	public class ProcessOutput(int code, string stdout, string stderr)
	{
		/// <summary>
		/// Gets the exit code returned by the process after it has finished executing.
		/// </summary>
		public int ExitCode { get; internal set; } = code;

		/// <summary>
		/// Gets the captured standard output produced by the process.
		/// </summary>
		public string StandardOutput { get; internal set; } = stdout;

		/// <summary>
		/// Gets the standard error output captured from the executed process.
		/// </summary>
		public string StandardError { get; internal set; } = stderr;
	}

	/// <summary>
	/// Runs an external process asynchronously with the specified arguments and timeout, capturing its exit code, standard output, and standard error.
	/// </summary>
	/// <remarks>
	/// If the process does not exit within the specified timeout, it is forcibly terminated.
	/// The method redirects both standard output and standard error streams for capture.
	/// This method is not thread-safe and should not be used for processes requiring interactive input.
	/// </remarks>
	/// <param name="fileName">The name or path of the executable file to run.</param>
	/// <param name="arguments">The command-line arguments to pass to the process; can be empty if no arguments are required.</param>
	/// <param name="timeout">The maximum duration to wait for the process to complete before terminating it; must be a positive time span.</param>
	/// <returns>
	/// A tuple containing the process exit code, the captured standard output, and the captured standard error.
	/// If the process times out, the exit code is <c>-1</c> and the standard error includes a timeout message.
	/// </returns>
	public static async Task<ProcessOutput>
		RunProcess(string fileName, string arguments, TimeSpan timeout)
	{
		var psi = new ProcessStartInfo
		{
			FileName = fileName,
			Arguments = arguments,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		using var proc = new Process { StartInfo = psi };
		var outputSb = new StringBuilder();
		var errorSb = new StringBuilder();

		proc.OutputDataReceived += (_, e) => { if (e.Data != null) outputSb.AppendLine(e.Data); };
		proc.ErrorDataReceived += (_, e) => { if (e.Data != null) errorSb.AppendLine(e.Data); };

		proc.Start();
		proc.BeginOutputReadLine();
		proc.BeginErrorReadLine();

		var exited = await Task.Run(() => proc.WaitForExit((int)timeout.TotalMilliseconds));
		if (!exited)
		{
			try { proc.Kill(); } catch { }
			return new(-1, outputSb.ToString(), errorSb.AppendLine("Process timed out").ToString());
		}

		return new(proc.ExitCode, outputSb.ToString(), errorSb.ToString());
	}
}
