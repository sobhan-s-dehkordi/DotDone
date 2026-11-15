using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DotDone.Core.Enums;
using DotDone.Core.Models;
using DotDone.Data.Context;

namespace DotDone.Services;

public record StreakGapResult(
    bool HasGap,
    int RepositoryId,
    string RepositoryTitle,
    int CurrentStreak,
    DateOnly LastActiveDate,
    List<DateOnly> MissedDays
);

public interface IStreakService
{
    Task<StreakGapResult?> CheckRepositoryGapAsync(int repositoryId, DateOnly? referenceDate = null);
    Task<List<StreakGapResult>> CheckAllGapsAsync(DateOnly? referenceDate = null);
    Task RecordActivityForStreakAsync(int repositoryId, ItemStatus newStatus, DateOnly? activityDate = null);
    Task ResetStreakAsync(int repositoryId);
    Task RecoverStreakAsync(int repositoryId, List<(int itemId, string itemType, DateOnly date, ItemStatus status)> retroactiveItems);
    Task RecalculateStreakAsync(int repositoryId);
}

public class StreakService : IStreakService
{
    private readonly DotDoneDbContext _db;

    public StreakService(DotDoneDbContext db)
    {
        _db = db;
    }

    public async Task<StreakGapResult?> CheckRepositoryGapAsync(int repositoryId, DateOnly? referenceDate = null)
    {
        var repo = await _db.Repositories.FindAsync(repositoryId);
        if (repo == null || !repo.IsStreakEnabled || repo.LastActiveDate == null || repo.CurrentStreak <= 0)
        {
            return null;
        }

        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.Now);
        var lastActive = repo.LastActiveDate.Value;

        // If active today or yesterday, streak is intact
        if (lastActive >= today.AddDays(-1))
        {
            return null;
        }

        // Gap detected!
        var missedDays = new List<DateOnly>();
        var current = lastActive.AddDays(1);
        while (current < today)
        {
            missedDays.Add(current);
            current = current.AddDays(1);
        }

        return new StreakGapResult(
            HasGap: true,
            RepositoryId: repo.Id,
            RepositoryTitle: repo.Title,
            CurrentStreak: repo.CurrentStreak,
            LastActiveDate: lastActive,
            MissedDays: missedDays
        );
    }

    public async Task<List<StreakGapResult>> CheckAllGapsAsync(DateOnly? referenceDate = null)
    {
        var repos = await _db.Repositories
            .Where(r => r.IsStreakEnabled && r.LastActiveDate != null && r.CurrentStreak > 0)
            .ToListAsync();

        var results = new List<StreakGapResult>();
        foreach (var repo in repos)
        {
            var gap = await CheckRepositoryGapAsync(repo.Id, referenceDate);
            if (gap != null)
            {
                results.Add(gap);
            }
        }
        return results;
    }

    public async Task RecordActivityForStreakAsync(int repositoryId, ItemStatus newStatus, DateOnly? activityDate = null)
    {
        // Only InProgress and Done count towards streak!
        if (newStatus != ItemStatus.InProgress && newStatus != ItemStatus.Done)
        {
            return;
        }

        var repo = await _db.Repositories.FindAsync(repositoryId);
        if (repo == null || !repo.IsStreakEnabled)
        {
            return;
        }

        var date = activityDate ?? DateOnly.FromDateTime(DateTime.Now);

        if (repo.LastActiveDate == null)
        {
            repo.CurrentStreak = 1;
            repo.LastActiveDate = date;
        }
        else if (repo.LastActiveDate == date)
        {
            // Already counted today
        }
        else if (repo.LastActiveDate == date.AddDays(-1))
        {
            repo.CurrentStreak += 1;
            repo.LastActiveDate = date;
        }
        else if (repo.LastActiveDate < date.AddDays(-1))
        {
            // Gap was not recovered; start streak afresh at 1
            repo.CurrentStreak = 1;
            repo.LastActiveDate = date;
        }

        await _db.SaveChangesAsync();
    }

    public async Task ResetStreakAsync(int repositoryId)
    {
        var repo = await _db.Repositories.FindAsync(repositoryId);
        if (repo != null)
        {
            repo.CurrentStreak = 0;
            repo.LastActiveDate = null;
            await _db.SaveChangesAsync();
        }
    }

    public async Task RecoverStreakAsync(int repositoryId, List<(int itemId, string itemType, DateOnly date, ItemStatus status)> retroactiveItems)
    {
        var repo = await _db.Repositories
            .Include(r => r.Projects)
                .ThenInclude(p => p.HeadTasks)
                    .ThenInclude(t => t.SubTasks)
            .FirstOrDefaultAsync(r => r.Id == repositoryId);

        if (repo == null || !repo.IsStreakEnabled)
        {
            return;
        }

        // Group retroactive items by date
        var dates = retroactiveItems
            .Where(x => x.status == ItemStatus.InProgress || x.status == ItemStatus.Done)
            .Select(x => x.date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        foreach (var item in retroactiveItems)
        {
            if (item.status != ItemStatus.InProgress && item.status != ItemStatus.Done)
                continue;

            int weight = item.status == ItemStatus.Done ? 2 : 1;

            if (item.itemType.Equals("Project", StringComparison.OrdinalIgnoreCase))
            {
                var prj = repo.Projects.FirstOrDefault(p => p.Id == item.itemId);
                if (prj != null)
                {
                    prj.Status = item.status;
                    prj.ModifiedDate = item.date.ToDateTime(TimeOnly.MinValue);
                    _db.ActivityLogs.Add(new DailyActivityLog
                    {
                        ActivityDate = item.date,
                        RepositoryId = repo.Id,
                        ProjectId = prj.Id,
                        StatusChangedTo = item.status,
                        ActivityWeight = weight,
                        RecordedAt = item.date.ToDateTime(TimeOnly.MinValue)
                    });
                }
            }
            else if (item.itemType.Equals("HeadTask", StringComparison.OrdinalIgnoreCase))
            {
                var task = repo.Projects.SelectMany(p => p.HeadTasks).FirstOrDefault(t => t.Id == item.itemId);
                if (task != null)
                {
                    task.Status = item.status;
                    task.ModifiedDate = item.date.ToDateTime(TimeOnly.MinValue);
                    _db.ActivityLogs.Add(new DailyActivityLog
                    {
                        ActivityDate = item.date,
                        RepositoryId = repo.Id,
                        ProjectId = task.ProjectId,
                        HeadTaskId = task.Id,
                        StatusChangedTo = item.status,
                        ActivityWeight = weight,
                        RecordedAt = item.date.ToDateTime(TimeOnly.MinValue)
                    });
                }
            }
            else if (item.itemType.Equals("SubTask", StringComparison.OrdinalIgnoreCase))
            {
                var sub = repo.Projects.SelectMany(p => p.HeadTasks).SelectMany(t => t.SubTasks).FirstOrDefault(s => s.Id == item.itemId);
                if (sub != null)
                {
                    sub.Status = item.status;
                    sub.ModifiedDate = item.date.ToDateTime(TimeOnly.MinValue);
                    var parentTask = repo.Projects.SelectMany(p => p.HeadTasks).FirstOrDefault(t => t.Id == sub.HeadTaskId);
                    _db.ActivityLogs.Add(new DailyActivityLog
                    {
                        ActivityDate = item.date,
                        RepositoryId = repo.Id,
                        ProjectId = parentTask?.ProjectId ?? 0,
                        HeadTaskId = parentTask?.Id,
                        SubTaskId = sub.Id,
                        StatusChangedTo = item.status,
                        ActivityWeight = weight,
                        RecordedAt = item.date.ToDateTime(TimeOnly.MinValue)
                    });
                }
            }
        }

        // Now calculate contiguous streak up to the latest date
        if (dates.Count > 0)
        {
            var maxDate = dates.Max();
            // Count additional continuous days covered
            var uniqueFilledDays = dates.Count;
            repo.CurrentStreak += uniqueFilledDays;
            if (repo.LastActiveDate == null || maxDate > repo.LastActiveDate.Value)
            {
                repo.LastActiveDate = maxDate;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task RecalculateStreakAsync(int repositoryId)
    {
        var repo = await _db.Repositories.FindAsync(repositoryId);
        if (repo == null || !repo.IsStreakEnabled)
        {
            return;
        }

        var activeDates = await _db.ActivityLogs
            .Where(l => l.RepositoryId == repositoryId && l.ActivityWeight > 0)
            .Select(l => l.ActivityDate)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        if (activeDates.Count == 0)
        {
            repo.CurrentStreak = 0;
            repo.LastActiveDate = null;
        }
        else
        {
            var lastDate = activeDates.Last();
            int streak = 1;
            for (int i = activeDates.Count - 2; i >= 0; i--)
            {
                if (activeDates[i] == activeDates[i + 1].AddDays(-1))
                {
                    streak++;
                }
                else
                {
                    break;
                }
            }
            repo.CurrentStreak = streak;
            repo.LastActiveDate = lastDate;
        }

        await _db.SaveChangesAsync();
    }
}
