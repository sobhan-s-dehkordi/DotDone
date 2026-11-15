using System;
using System.Threading.Tasks;
using FluentAssertions;
using DotDone.Core.Enums;
using DotDone.Core.Models;
using DotDone.Services;
using DotDone.Data.Services;
using Xunit;

namespace DotDone.Tests;

public class ActivityHeatmapTests
{
    [Fact]
    public async Task Heatmap_ShouldWeighDoneTwiceAsMuchAsInProgress()
    {
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository { Title = "Heatmap Repo" };
        var proj = new Project { Repository = repo, Title = "Main Project" };
        var task1 = new HeadTask { Project = proj, Title = "Task 1" };
        var task2 = new HeadTask { Project = proj, Title = "Task 2" };

        db.Repositories.Add(repo);
        db.Projects.Add(proj);
        db.HeadTasks.AddRange(task1, task2);
        await db.SaveChangesAsync();

        var streakService = new StreakService(db);
        var fileStorage = new FileStorageService("test_attachments");
        var taskService = new TaskService(db, fileStorage, streakService);

        // Act: Task 1 set to InProgress (weight 1)
        await taskService.UpdateHeadTaskStatusAsync(task1.Id, ItemStatus.InProgress);

        // Task 2 set to Done (weight 2)
        await taskService.UpdateHeadTaskStatusAsync(task2.Id, ItemStatus.Done);

        // Check Heatmap
        var heatmapService = new ActivityHeatmapService(db);
        var today = DateOnly.FromDateTime(DateTime.Now);
        var summary = await heatmapService.GetActivityHeatmapAsync(repo.Id, null, today, today);

        // Assert
        summary.TotalActiveDays.Should().Be(1);
        summary.TotalWeight.Should().Be(3); // 1 + 2 = 3
        summary.TotalDoneCount.Should().Be(1);
        summary.TotalInProgressCount.Should().Be(1);

        var todaySummary = summary.Days[0];
        todaySummary.TotalWeight.Should().Be(3);
        todaySummary.IntensityLevel.Should().Be(2); // Weight 3 is intensity 2
    }

    [Fact]
    public async Task Heatmap_ShouldFilter_ByProject()
    {
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository { Title = "Multi Project Repo" };
        var projA = new Project { Repository = repo, Title = "Project A" };
        var projB = new Project { Repository = repo, Title = "Project B" };
        var taskA = new HeadTask { Project = projA, Title = "Task A" };
        var taskB = new HeadTask { Project = projB, Title = "Task B" };

        db.Repositories.Add(repo);
        db.Projects.AddRange(projA, projB);
        db.HeadTasks.AddRange(taskA, taskB);
        await db.SaveChangesAsync();

        var streakService = new StreakService(db);
        var fileStorage = new FileStorageService("test_attachments");
        var taskService = new TaskService(db, fileStorage, streakService);

        await taskService.UpdateHeadTaskStatusAsync(taskA.Id, ItemStatus.Done); // Proj A: weight 2
        await taskService.UpdateHeadTaskStatusAsync(taskB.Id, ItemStatus.InProgress); // Proj B: weight 1

        var heatmapService = new ActivityHeatmapService(db);
        var today = DateOnly.FromDateTime(DateTime.Now);

        // Filter specifically for Project A
        var projASummary = await heatmapService.GetActivityHeatmapAsync(null, projA.Id, today, today);
        projASummary.TotalWeight.Should().Be(2);
        projASummary.TotalDoneCount.Should().Be(1);
        projASummary.TotalInProgressCount.Should().Be(0);

        // Filter for entire repository
        var repoSummary = await heatmapService.GetActivityHeatmapAsync(repo.Id, null, today, today);
        repoSummary.TotalWeight.Should().Be(3); // 2 + 1
    }

    [Fact]
    public async Task Heatmap_DoesNotReset_WhenStreakResets()
    {
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository { Title = "Resettable Streak Repo", IsStreakEnabled = true };
        var proj = new Project { Repository = repo, Title = "Sprint" };
        var task = new HeadTask { Project = proj, Title = "Task" };

        db.Repositories.Add(repo);
        db.Projects.Add(proj);
        db.HeadTasks.Add(task);
        await db.SaveChangesAsync();

        var streakService = new StreakService(db);
        var fileStorage = new FileStorageService("test_attachments");
        var taskService = new TaskService(db, fileStorage, streakService);

        // Perform activity
        await taskService.UpdateHeadTaskStatusAsync(task.Id, ItemStatus.Done);

        // Now streak is reset to 0
        await streakService.ResetStreakAsync(repo.Id);
        var refreshedRepo = await db.Repositories.FindAsync(repo.Id);
        refreshedRepo!.CurrentStreak.Should().Be(0);

        // But historical Heatmap logs remain untouched!
        var heatmapService = new ActivityHeatmapService(db);
        var today = DateOnly.FromDateTime(DateTime.Now);
        var summary = await heatmapService.GetActivityHeatmapAsync(repo.Id, null, today, today);

        summary.TotalActiveDays.Should().Be(1);
        summary.TotalWeight.Should().Be(2);
    }

    [Fact]
    public async Task Heatmap_ShouldRemoveActivityWeight_WhenTaskRevertedToTodo()
    {
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository { Title = "Revert Repo", IsStreakEnabled = true };
        var proj = new Project { Repository = repo, Title = "Revert Proj" };
        var task1 = new HeadTask { Project = proj, Title = "Task 1" };
        var task2 = new HeadTask { Project = proj, Title = "Task 2" };

        db.Repositories.Add(repo);
        db.Projects.Add(proj);
        db.HeadTasks.AddRange(task1, task2);
        await db.SaveChangesAsync();

        var streakService = new StreakService(db);
        var fileStorage = new FileStorageService("test_attachments");
        var taskService = new TaskService(db, fileStorage, streakService);
        var heatmapService = new ActivityHeatmapService(db);
        var today = DateOnly.FromDateTime(DateTime.Now);

        // Step 1: Mark task1 Done (weight 2) and task2 InProgress (weight 1)
        await taskService.UpdateHeadTaskStatusAsync(task1.Id, ItemStatus.Done);
        await taskService.UpdateHeadTaskStatusAsync(task2.Id, ItemStatus.InProgress);

        var summary1 = await heatmapService.GetActivityHeatmapAsync(repo.Id, null, today, today);
        summary1.TotalWeight.Should().Be(3);
        summary1.TotalDoneCount.Should().Be(1);
        summary1.TotalInProgressCount.Should().Be(1);
        summary1.TotalActiveDays.Should().Be(1);

        var repoDb = await db.Repositories.FindAsync(repo.Id);
        repoDb!.CurrentStreak.Should().Be(1);

        // Step 2: Revert task1 to Todo (unconfirmed)
        await taskService.UpdateHeadTaskStatusAsync(task1.Id, ItemStatus.Todo);

        var summary2 = await heatmapService.GetActivityHeatmapAsync(repo.Id, null, today, today);
        summary2.TotalWeight.Should().Be(1);
        summary2.TotalDoneCount.Should().Be(0);
        summary2.TotalInProgressCount.Should().Be(1);
        summary2.TotalActiveDays.Should().Be(1);

        // Step 3: Revert task2 to Todo
        await taskService.UpdateHeadTaskStatusAsync(task2.Id, ItemStatus.Todo);

        var summary3 = await heatmapService.GetActivityHeatmapAsync(repo.Id, null, today, today);
        summary3.TotalWeight.Should().Be(0);
        summary3.TotalDoneCount.Should().Be(0);
        summary3.TotalInProgressCount.Should().Be(0);
        summary3.TotalActiveDays.Should().Be(0);

        repoDb = await db.Repositories.FindAsync(repo.Id);
        repoDb!.CurrentStreak.Should().Be(0);
        repoDb.LastActiveDate.Should().BeNull();
    }
}
