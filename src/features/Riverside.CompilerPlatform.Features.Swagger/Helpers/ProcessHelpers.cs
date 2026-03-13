using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Riverside.CompilerPlatform.Features.Swagger.Helpers;

/// <summary>
/// Provides helper methods for running external processes and capturing their output and exit codes.
/// </summary>
public static class ProcessHelpers
{
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
	public static async Task<(int ExitCode, string StandardOutput, string StandardError)>
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
			return (-1, outputSb.ToString(), errorSb.AppendLine("Process timed out").ToString());
		}

		return (proc.ExitCode, outputSb.ToString(), errorSb.ToString());
	}
}
