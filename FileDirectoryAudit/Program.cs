using FileDirectoryAudit;
using System.Collections.Concurrent;
using System.Diagnostics;

class Program {
    private static string? directoryPath;
    private static string? outputCsvPath;
    private static ConsoleColor defaultForeground;

    public static async Task Main(string[] args) {
        defaultForeground = Console.ForegroundColor;
#if DEBUG
        directoryPath = @"C:\Users\pcromer\Documents";
#else
        start:
        Console.WriteLine("Enter your target directory: ");
        directoryPath = Console.ReadLine();

        if (string.IsNullOrEmpty(directoryPath)) {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Directory cannot be empty!");
            Console.ForegroundColor = defaultForeground;
            Console.WriteLine();
            goto start;
        }
#endif

        Console.WriteLine();
        Console.Write("Scanning directory: ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(directoryPath);
        Console.ForegroundColor = defaultForeground;

        var fileInfoBag = new ConcurrentBag<FileInfoResult>();
        var watch = Stopwatch.StartNew();

        // Asyncronsouly run the Task
        await Task.Run(() => {
            try {
                if (!Directory.Exists(directoryPath)) {
                    Console.WriteLine("Directory does not exist or access is denied.");
                    return;
                }

                // Run the thread in parallel safely enumerating throught each directory and file
                Parallel.ForEach(Helpers.EnumerateFilesSafely(directoryPath, defaultForeground), filePath => {
                    if (!Helpers.HasAccess(filePath)) {
                        fileInfoBag.Add(new FileInfoResult {
                            FullFilePath = filePath
                        });
                        return;
                    }

                    try {
                        var fileInfo = new FileInfo(filePath);

                        fileInfoBag.Add(new FileInfoResult {
                            FileDirectory = fileInfo.DirectoryName,
                            FileName = fileInfo.Name,
                            FullFilePath = fileInfo.FullName,
                            FileSizeBytes = fileInfo.Length,
                            // LastAccessed = fileInfo.LastAccessTime, // Last Accessed DateTime will always be DateTime of this program being ran
                            LastModified = fileInfo.LastWriteTime,
                            FirstCreated = fileInfo.CreationTime,
                            LastModifiedBy = Helpers.GetLastModifiedBy(fileInfo)
                        });

                        if (!string.IsNullOrWhiteSpace(filePath)) {
                            Console.Write("Found: ");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(fileInfo.FullName);
                            Console.ForegroundColor = defaultForeground;
                        }
                    }
                    catch {
                        fileInfoBag.Add(new FileInfoResult {
                            FullFilePath = filePath
                        });
                    }
                });
            }
            catch (UnauthorizedAccessException ex) {
                Console.Write("Access denied: ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = defaultForeground;
            }
            catch (Exception ex) {
                Console.Write("Error scanning directory: ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = defaultForeground;
            }
        });
        watch.Stop();

        /*
         * - Put in same folder as program .exe
         * - Set file DateTime now (at the end) instead of beginning of the program
         */
        outputCsvPath = $"{AppDomain.CurrentDomain.BaseDirectory}File_Audit_output-{$"{DateTime.Now:s}".Replace(':', '_')}.csv";

        // Save everything in an CSV file
        if (!fileInfoBag.IsEmpty && !string.IsNullOrWhiteSpace(outputCsvPath)) {
            Console.WriteLine("Writing results to CSV...");
            await WriteCsvAsync(outputCsvPath, fileInfoBag);

            Console.WriteLine($"Operation completed.");
            Console.Write("Scanned: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(fileInfoBag.Count.ToString());
            Console.ForegroundColor = defaultForeground;
            Console.WriteLine(" files");

            Console.Write("Results saved to: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(outputCsvPath);
            Console.ForegroundColor = defaultForeground;

            Console.Write($"With a time of: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(watch.Elapsed.ToString(@"hh\:mm\:ss"));
            Console.ForegroundColor = defaultForeground;
            Console.WriteLine(" (HH:MM:SS)");
            
        }
        else Console.WriteLine("No files were processed due to access issues or an empty directory.");

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static async Task WriteCsvAsync(string filePath, IEnumerable<FileInfoResult> fileInfoResult) {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using var writer = new StreamWriter(filePath);
        await writer.WriteLineAsync("file_directory,file_name,full_file_path,file_size_bytes,last_modified,first_created,last_modified_by");

        foreach (var fileInfo in fileInfoResult.OrderBy(f => f.LastModified)) {
            await writer.WriteLineAsync(
                $"{Helpers.EscapeCsv(fileInfo.FileDirectory!)}," +
                $"{Helpers.EscapeCsv(fileInfo.FileName!)}," +
                $"{Helpers.EscapeCsv(fileInfo.FullFilePath!)}," +
                $"{fileInfo.FileSizeBytes}," +
                $"{fileInfo.LastModified:s}," +
                $"{fileInfo.FirstCreated:s}," +
                $"{Helpers.EscapeCsv(fileInfo.LastModifiedBy!)}"
            );
        }
    }
}