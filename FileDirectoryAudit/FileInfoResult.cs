namespace FileDirectoryAudit;

internal class FileInfoResult {
    public string? FileDirectory { get; set; }
    public string? FileName { get; set; }
    public string? FullFilePath { get; set; }
    public long FileSizeBytes { get; set; }
    // public DateTime LastAccessed { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime FirstCreated { get; set; }
    public string? LastModifiedBy { get; set; }
}
