using System;
using System.Collections.Generic;

namespace DotDone.Core.Models;

public class Repository
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsStreakEnabled { get; set; } = false;
    public int CurrentStreak { get; set; } = 0;
    public DateOnly? LastActiveDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public List<Project> Projects { get; set; } = new();
    public List<DailyActivityLog> ActivityLogs { get; set; } = new();
}
