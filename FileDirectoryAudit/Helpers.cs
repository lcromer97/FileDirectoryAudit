using System.Security.Principal;

namespace FileDirectoryAudit;

internal static class Helpers {
    /// <summary>
    /// Checks if the string contains multiple character values using OR
    /// </summary>
    /// <param name="str1">this</param>
    /// <param name="chars">As many characters as you want to compare to the target string</param>
    public static bool OrContainsMultiple(this string str1, params char[] chars) => chars.Any(str1.ToLower().Contains);

    /// <summary>
    /// Ensures files and directories are able to be accessed safely by the user
    /// </summary>
    public static IEnumerable<string> EnumerateFilesSafely(string directoryPath, ConsoleColor defaultForeground) {
        var pendingDirectories = new Stack<string>();
        var files = new List<string>();
        pendingDirectories.Push(directoryPath);

        while (pendingDirectories.Count > 0) {
            var currentDirectory = pendingDirectories.Pop();
            try {
                foreach (var subDirectory in Directory.EnumerateDirectories(currentDirectory)) {
                    pendingDirectories.Push(subDirectory);
                }

                files.AddRange(Directory.EnumerateFiles(currentDirectory));
            }
            catch (UnauthorizedAccessException) {
                Console.WriteLine("Access denied to directory: ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(currentDirectory);
                Console.ForegroundColor = defaultForeground;
            }
            catch (Exception ex) {
                Console.WriteLine("Error accessing directory ");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write(currentDirectory);
                Console.ForegroundColor = defaultForeground;
                Console.Write(" : ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = defaultForeground;
            }
        }

        return files;
    }

    /// <summary>
    /// Check if the file, in the full patch given, can be accessed
    /// </summary>
    public static bool HasAccess(string filePath) {
        try {
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read);
            return true;
        }
        catch (UnauthorizedAccessException) {
            return false;
        }
        catch {
            return false;
        }
    }

    /// <summary>
    /// Gets the last windows user that modified the given file (DOMAIN\User)
    /// </summary>
    public static string GetLastModifiedBy(FileInfo fileInfo) {
        try {
            var fileSecurity = fileInfo.GetAccessControl();
            var identity = fileSecurity.GetOwner(typeof(NTAccount));
            return identity?.Value ?? "";
        }
        catch {
            return ""; // Return empty if the owner cannot be determined
        }
    }

    /// <summary>
    /// Escape characters that will break CSV formatting
    /// </summary>
    public static string EscapeCsv(string value) {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.OrContainsMultiple(',', '"', '\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
