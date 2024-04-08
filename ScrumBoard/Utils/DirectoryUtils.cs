using System;
using System.IO;
using System.IO.Abstractions;
using Microsoft.VisualBasic.FileIO;

namespace ScrumBoard.Utils;

public static class DirectoryUtils
{
    /// <summary>
    /// Determines whether some file exists directly in some directory, not in a parent or child directory.
    /// Useful for detecting attempted path traversal if allowing users to input filenames.
    /// </summary>
    /// <param name="fileSystem">The current file system in use</param>
    /// <param name="absolutePathToFile">Absolute path to the file in question</param>
    /// <param name="absolutePathToDirectory">Absolute path of the directory for which we are checking if it contains the file</param>
    /// <returns>True if the given file exists directly in the given directory, false otherwise</returns>
    public static bool FileExistsInDirectory(this IFileSystem fileSystem, string absolutePathToFile, string absolutePathToDirectory)
    {
        var file = fileSystem.FileInfo.New(absolutePathToFile);
        var directory = fileSystem.DirectoryInfo.New(absolutePathToDirectory);
        if (file.Directory is null || !file.Exists || !directory.Exists) return false;
        return file.Directory.FullName.TrimEnd('/').TrimEnd('\\').Equals(directory.FullName.TrimEnd('/').TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase);
    }
}