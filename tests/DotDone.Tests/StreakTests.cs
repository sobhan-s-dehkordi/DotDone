using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using DotDone.Core.Enums;
using DotDone.Core.Models;
using DotDone.Services;
using Xunit;

namespace DotDone.Tests;

public class StreakTests
{
    [Fact]
    public async Task Streak_ShouldOnlyIncrement_ForInProgress_And_Done()
    {
        // Arrange
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository { Title = "Duolingo Persian Repo", IsStreakEnabled = true };
        db.Repositories.Add(repo);
        await db.SaveChangesAsync();

        var streakService = new StreakService(db);
        var day1 = new DateOnly(2026, 11, 1);

        // Act 1: Todo status should NOT increment streak
        await streakService.RecordActivityForStreakAsync(repo.Id, ItemStatus.Todo, day1);
        repo = await db.Repositories.FindAsync(repo.Id);
        repo!.CurrentStreak.Should().Be(0);
        repo.LastActiveDate.Should().BeNull();

        // Act 2: InProgress status should start streak at 1
        await streakService.RecordActivityForStreakAsync(repo.Id, ItemStatus.InProgress, day1);
        repo = await db.Repositories.FindAsync(repo.Id);
        repo!.CurrentStreak.Should().Be(1);
        repo.LastActiveDate.Should().Be(day1);

        // Act 3: Same day second activity (Done) should NOT double increment streak
        await streakService.RecordActivityForStreakAsync(repo.Id, ItemStatus.Done, day1);
        repo = await db.Repositories.FindAsync(repo.Id);
        repo!.CurrentStreak.Should().Be(1);

        // Act 4: Next consecutive day activity should increment to 2
        var day2 = day1.AddDays(1);
        await streakService.RecordActivityForStreakAsync(repo.Id, ItemStatus.Done, day2);
        repo = await db.Repositories.FindAsync(repo.Id);
        repo!.CurrentStreak.Should().Be(2);
        repo.LastActiveDate.Should().Be(day2);
    }

    [Fact]
    public async Task GapDetection_ShouldDetectMissedDays_WhenUserIsInactive()
    {
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository
        {
            Title = "Study",
            IsStreakEnabled = true,
            CurrentStreak = 5,
            LastActiveDate = new DateOnly(2026, 11, 1)
        };
        db.Repositories.Add(repo);
        await db.SaveChangesAsync();

        var streakService = new StreakService(db);

        // Reference date is 4 days later (Nov 5)
        var today = new DateOnly(2026, 11, 5);
        var gap = await streakService.CheckRepositoryGapAsync(repo.Id, today);

        // Assert
        gap.Should().NotBeNull();
        gap!.HasGap.Should().BeTrue();
        gap.CurrentStreak.Should().Be(5);
        gap.LastActiveDate.Should().Be(new DateOnly(2026, 11, 1));
        gap.MissedDays.Should().Equal(
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 11, 3),
            new DateOnly(2026, 11, 4)
        );
    }

    [Fact]
    public async Task ResetStreak_ShouldSetCurrentStreakToZero()
    {
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository
        {
            Title = "Workout",
            IsStreakEnabled = true,
            CurrentStreak = 10,
            LastActiveDate = new DateOnly(2026, 11, 1)
        };
        db.Repositories.Add(repo);
        await db.SaveChangesAsync();

        var streakService = new StreakService(db);
        await streakService.ResetStreakAsync(repo.Id);

        var updated = await db.Repositories.FindAsync(repo.Id);
        updated!.CurrentStreak.Should().Be(0);
        updated.LastActiveDate.Should().BeNull();
    }

    [Fact]
    public async Task RecoverStreak_ShouldRetroactivelyFillActivity_AndPreserveStreak()
    {
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository
        {
            Title = "Coding",
            IsStreakEnabled = true,
            CurrentStreak = 3,
            LastActiveDate = new DateOnly(2026, 11, 1)
        };
        var proj = new Project { Repository = repo, Title = "DotDone" };
        var task = new HeadTask { Project = proj, Title = "WinUI Implementation", Status = ItemStatus.Todo };
        db.Repositories.Add(repo);
        db.Projects.Add(proj);
        db.HeadTasks.Add(task);
        await db.SaveChangesAsync();

        var streakService = new StreakService(db);

        // Offline on Nov 2 and Nov 3, user retroactively confirms completing tasks on those days
        var retroactiveItems = new List<(int itemId, string itemType, DateOnly date, ItemStatus status)>
        {
            (task.Id, "HeadTask", new DateOnly(2026, 11, 2), ItemStatus.InProgress),
            (task.Id, "HeadTask", new DateOnly(2026, 11, 3), ItemStatus.Done)
        };

        await streakService.RecoverStreakAsync(repo.Id, retroactiveItems);

        // Assert
        var updatedRepo = await db.Repositories.FindAsync(repo.Id);
        updatedRepo!.CurrentStreak.Should().Be(5); // 3 + 2 filled days
        updatedRepo.LastActiveDate.Should().Be(new DateOnly(2026, 11, 3));

        // Verify Activity Logs were registered for those days
        var logs = await db.ActivityLogs.ToListAsync();
        logs.Should().HaveCount(2);
        logs.Should().Contain(l => l.ActivityDate == new DateOnly(2026, 11, 2) && l.ActivityWeight == 1);
        logs.Should().Contain(l => l.ActivityDate == new DateOnly(2026, 11, 3) && l.ActivityWeight == 2);
    }
}
