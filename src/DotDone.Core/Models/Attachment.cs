using System;

namespace DotDone.Core.Models;

public class Attachment
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    // Optional relationships
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? HeadTaskId { get; set; }
    public HeadTask? HeadTask { get; set; }

    public int? SubTaskId { get; set; }
    public SubTask? SubTask { get; set; }
}
