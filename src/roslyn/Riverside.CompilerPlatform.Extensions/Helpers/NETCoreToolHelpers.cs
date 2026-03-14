using System;
using System.IO;
using System.Threading.Tasks;

namespace Riverside.CompilerPlatform.Helpers;

/// <summary>
/// Provides helpers for installing and locating .NET tools installed to a specific tool-path directory.
/// </summary>
public static class NETCoreToolHelpers
{
	/// <summary>
	/// Returns the full path to the tool executable expected in <paramref name="toolDirectory"/>.
	/// Appends <c>.exe</c> on Windows; uses the bare name on all other platforms.
	/// </summary>
	/// <param name="toolDirectory">The directory the tool was installed into via <c>--tool-path</c>.</param>
	/// <param name="toolName">The tool executable name (e.g. <c>kiota</c>).</param>
	/// <returns>The full path including the platform-appropriate extension.</returns>
	public static string GetExecutablePath(string toolDirectory, string toolName)
		=> Path.Combine(
			toolDirectory,
			Environment.OSVersion.Platform == PlatformID.Win32NT ? toolName + ".exe" : toolName);

	/// <summary>
	/// Ensures the specified .NET tool is available in <paramref name="toolDirectory"/>.
	/// </summary>
	/// <remarks>
	/// <list type="bullet">
	///		<item>If the executable already exists and no specific <paramref name="version"/> is requested, the tool is reused immediately.</item>
	///		<item>Otherwise <c>dotnet tool install</c> is attempted. If it fails because the tool is already installed, <c>dotnet tool update</c> is tried instead.</item>
	///		<item>Installation succeeds when the executable is present after the above steps.</item>
	/// </list>
	/// </remarks>
	/// <param name="packageId">The NuGet package ID of the tool (e.g. <c>Riverside.JsonBinder.Console</c>).</param>
	/// <param name="toolDirectory">The directory to install the tool into, passed to <c>--tool-path</c>.</param>
	/// <param name="version">
	/// A specific version to pin. Pass <see langword="null"/> to install or keep the latest.
	/// </param>
	/// <param name="timeout">Maximum wait time per install or update process. Defaults to 5 minutes.</param>
	/// <param name="commandName">
	/// The executable/command name to look for in <paramref name="toolDirectory"/> (e.g. <c>jsonbinder</c>).
	/// If <see langword="null"/>, <paramref name="packageId"/> is used as the command name.
	/// </param>
	/// <returns>
	/// A tuple where <c>Success</c> is <see langword="true"/> when the executable is available, and <c>Error</c> carries the captured stderr when installation fails.
	/// </returns>
	public static async Task<(bool Success, string? Error)> EnsureToolAsync(
		string packageId,
		string toolDirectory,
		string? version = null,
		TimeSpan? timeout = null,
		string? commandName = null)
	{
		var exeName = string.IsNullOrWhiteSpace(commandName) ? packageId : commandName;
		var exe = GetExecutablePath(toolDirectory, exeName!);
		var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);

		Directory.CreateDirectory(toolDirectory);

		if (File.Exists(exe) && string.IsNullOrWhiteSpace(version))
			return (true, null);

		var toolPathArg = $"--tool-path \"{toolDirectory}\"";
		var versionArg = string.IsNullOrWhiteSpace(version) ? string.Empty : $" --version {version}";

		var installResult = await ProcessHelpers.RunNETCoreCliAsync(
			$"tool install {packageId} {toolPathArg}{versionArg}", effectiveTimeout);

		if (installResult.ExitCode == 0)
			return (true, null);

		// install exits non-zero when the tool is already present; attempt an update instead
		var updateResult = await ProcessHelpers.RunNETCoreCliAsync(
			$"tool update {packageId} {toolPathArg}{versionArg}", effectiveTimeout);

		return File.Exists(exe)
			? (true, null)
			: (false, updateResult.StandardError);
	}
}
