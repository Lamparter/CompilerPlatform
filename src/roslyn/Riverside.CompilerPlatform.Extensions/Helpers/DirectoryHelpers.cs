using System;
using System.IO;

namespace Riverside.CompilerPlatform.Helpers;

/// <summary>
/// Provides utility methods for common directory operations.
/// </summary>
public static class DirectoryHelpers
{
    /// <summary>
    /// Deletes <paramref name="path"/> and all of its contents recursively, suppressing any exception that occurs.
    /// </summary>
    /// <param name="path">The directory to delete.</param>
    public static void TryDelete(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch { }
    }

    /// <summary>
    /// Creates a uniquely named subdirectory under <paramref name="basePath"/> and returns its full path.
    /// The subdirectory name is a compact <see cref="Guid"/> with no formatting characters.
    /// </summary>
    /// <param name="basePath">The parent directory. Created if it does not already exist.</param>
    /// <returns>The full path of the newly created temporary directory.</returns>
    public static string CreateTemporary(string basePath)
    {
        var path = Path.Combine(basePath, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
