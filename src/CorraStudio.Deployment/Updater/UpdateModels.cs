namespace CorraStudio.Deployment.Updater;

public class UpdateInfo
{
    public string Version { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string ReleaseNotes { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string InstallerUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public List<UpdateFile> Files { get; set; } = new();
}

public class UpdateFile
{
    public string Name { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public long Size { get; set; }
}

public class UpdateCheckResult
{
    public bool HasUpdate { get; set; }
    public UpdateInfo? LatestVersion { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UpdateProgress
{
    public int ProgressPercentage { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public UpdateStatus Status { get; set; }
}

public enum UpdateStatus
{
    Idle = 0,
    Checking = 1,
    Downloading = 2,
    Installing = 3,
    Complete = 4,
    Failed = 5
}
