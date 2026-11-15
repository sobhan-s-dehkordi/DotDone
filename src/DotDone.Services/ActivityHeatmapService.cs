using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DotDone.Core.Models;
using DotDone.Data.Context;

namespace DotDone.Services;

public record HeatmapDayDto(
    DateOnly Date,
    int TotalWeight,
    int DoneCount,
    int InProgressCount,
    int IntensityLevel // 0 to 4
);

public record HeatmapSummaryDto(
    int TotalActiveDays,
    int TotalWeight,
    int TotalDoneCount,
    int TotalInProgressCount,
    List<HeatmapDayDto> Days
);

public interface IActivityHeatmapService
{
    Task<HeatmapSummaryDto> GetActivityHeatmapAsync(
        int? repositoryId = null,
        int? projectId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null);
}

public class ActivityHeatmapService : IActivityHeatmapService
{
    private readonly DotDoneDbContext _db;

    public ActivityHeatmapService(DotDoneDbContext db)
    {
        _db = db;
    }

    public async Task<HeatmapSummaryDto> GetActivityHeatmapAsync(
        int? repositoryId = null,
        int? projectId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        var end = endDate ?? DateOnly.FromDateTime(DateTime.Now);
        // Default 52 weeks back (approx 364 days)
        var start = startDate ?? end.AddDays(-364);

        var query = _db.ActivityLogs.AsNoTracking()
            .Where(l => l.ActivityDate >= start && l.ActivityDate <= end);

        if (projectId.HasValue)
        {
            query = query.Where(l => l.ProjectId == projectId.Value);
        }
        else if (repositoryId.HasValue)
        {
            query = query.Where(l => l.RepositoryId == repositoryId.Value);
        }

        var logs = await query.ToListAsync();

        var grouped = logs
            .GroupBy(l => l.ActivityDate)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    TotalWeight = g.Sum(x => x.ActivityWeight),
                    DoneCount = g.Count(x => x.StatusChangedTo == Core.Enums.ItemStatus.Done),
                    InProgressCount = g.Count(x => x.StatusChangedTo == Core.Enums.ItemStatus.InProgress)
                }
            );

        var days = new List<HeatmapDayDto>();
        var cur = start;
        int totalActiveDays = 0;
        int grandTotalWeight = 0;
        int grandDoneCount = 0;
        int grandInProgressCount = 0;

        while (cur <= end)
        {
            if (grouped.TryGetValue(cur, out var stat))
            {
                totalActiveDays++;
                grandTotalWeight += stat.TotalWeight;
                grandDoneCount += stat.DoneCount;
                grandInProgressCount += stat.InProgressCount;

                days.Add(new HeatmapDayDto(
                    Date: cur,
                    TotalWeight: stat.TotalWeight,
                    DoneCount: stat.DoneCount,
                    InProgressCount: stat.InProgressCount,
                    IntensityLevel: CalculateIntensity(stat.TotalWeight)
                ));
            }
            else
            {
                days.Add(new HeatmapDayDto(
                    Date: cur,
                    TotalWeight: 0,
                    DoneCount: 0,
                    InProgressCount: 0,
                    IntensityLevel: 0
                ));
            }
            cur = cur.AddDays(1);
        }

        return new HeatmapSummaryDto(
            TotalActiveDays: totalActiveDays,
            TotalWeight: grandTotalWeight,
            TotalDoneCount: grandDoneCount,
            TotalInProgressCount: grandInProgressCount,
            Days: days
        );
    }

    private static int CalculateIntensity(int weight)
    {
        return weight switch
        {
            0 => 0,
            >= 1 and <= 2 => 1,
            >= 3 and <= 5 => 2,
            >= 6 and <= 9 => 3,
            _ => 4
        };
    }
}
