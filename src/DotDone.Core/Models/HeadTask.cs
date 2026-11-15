using System;
using System.Collections.Generic;
using DotDone.Core.Enums;

namespace DotDone.Core.Models;

public class HeadTask
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ItemStatus Status { get; set; } = ItemStatus.Todo;
    public string? ReviewNotes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }

    public List<SubTask> SubTasks { get; set; } = new();
    public List<Attachment> Attachments { get; set; } = new();
}
