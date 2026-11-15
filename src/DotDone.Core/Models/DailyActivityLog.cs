using System;
using DotDone.Core.Enums;

namespace DotDone.Core.Models;

public class DailyActivityLog
{
    public int Id { get; set; }
    public DateOnly ActivityDate { get; set; }
    public int RepositoryId { get; set; }
    public Repository? Repository { get; set; }

    public int ProjectId { get; set; }
    public int? HeadTaskId { get; set; }
    public int? SubTaskId { get; set; }

    public ItemStatus StatusChangedTo { get; set; }
    public int ActivityWeight { get; set; } // 1 for InProgress, 2 for Done

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
