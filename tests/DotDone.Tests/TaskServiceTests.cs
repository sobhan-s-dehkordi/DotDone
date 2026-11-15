using System;
using System.Threading.Tasks;
using FluentAssertions;
using DotDone.Core.Enums;
using DotDone.Core.Models;
using DotDone.Services;
using DotDone.Data.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotDone.Tests;

public class TaskServiceTests
{
    [Fact]
    public async Task CreateProject_ShouldTruncateDescriptionsOver500Chars()
    {
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository { Title = "Repo" };
        db.Repositories.Add(repo);
        await db.SaveChangesAsync();

        var streakService = new StreakService(db);
        var fileStorage = new FileStorageService("test_attachments");
        var taskService = new TaskService(db, fileStorage, streakService);

        var longDesc = new string('A', 600);
        var project = await taskService.CreateProjectAsync(repo.Id, "Short Title", longDesc);

        project.Title.Should().Be("Short Title");
        project.Description.Should().HaveLength(500);
    }

    [Fact]
    public async Task DeleteRepository_ShouldCascadeDelete_ProjectsAndTasks()
    {
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository { Title = "Temporary Repo" };
        var proj = new Project { Repository = repo, Title = "Temp Project" };
        var task = new HeadTask { Project = proj, Title = "Temp Task" };
        var sub = new SubTask { HeadTask = task, Title = "Temp Sub" };

        db.Repositories.Add(repo);
        db.Projects.Add(proj);
        db.HeadTasks.Add(task);
        db.SubTasks.Add(sub);
        await db.SaveChangesAsync();

        var streakService = new StreakService(db);
        var fileStorage = new FileStorageService("test_attachments");
        var taskService = new TaskService(db, fileStorage, streakService);

        // Act
        await taskService.DeleteRepositoryAsync(repo.Id);

        // Assert
        (await db.Repositories.CountAsync()).Should().Be(0);
        (await db.Projects.CountAsync()).Should().Be(0);
        (await db.HeadTasks.CountAsync()).Should().Be(0);
        (await db.SubTasks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SubTaskStatusUpdate_ShouldTriggerParentRepoStreak()
    {
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository { Title = "Active Repo", IsStreakEnabled = true };
        var proj = new Project { Repository = repo, Title = "Active Proj" };
        var task = new HeadTask { Project = proj, Title = "Active Task" };
        var sub = new SubTask { HeadTask = task, Title = "Active SubTask", Status = ItemStatus.Todo };

        db.Repositories.Add(repo);
        db.Projects.Add(proj);
        db.HeadTasks.Add(task);
        db.SubTasks.Add(sub);
        await db.SaveChangesAsync();

        var streakService = new StreakService(db);
        var fileStorage = new FileStorageService("test_attachments");
        var taskService = new TaskService(db, fileStorage, streakService);

        // Act: Update subtask to Done
        await taskService.UpdateSubTaskStatusAsync(sub.Id, ItemStatus.Done);

        // Assert
        var refreshedRepo = await db.Repositories.FindAsync(repo.Id);
        refreshedRepo!.CurrentStreak.Should().Be(1);
        refreshedRepo.LastActiveDate.Should().Be(DateOnly.FromDateTime(DateTime.Now));

        var log = await db.ActivityLogs.FirstOrDefaultAsync(l => l.SubTaskId == sub.Id);
        log.Should().NotBeNull();
        log!.ActivityWeight.Should().Be(2); // Done is weight 2
    }

    [Fact]
    public async Task UpdateRepository_ShouldUpdateTitleDescriptionAndStreak()
    {
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository { Title = "Initial Repo", Description = "Old", IsStreakEnabled = false };
        db.Repositories.Add(repo);
        await db.SaveChangesAsync();

        var streakService = new StreakService(db);
        var fileStorage = new FileStorageService("test_attachments");
        var taskService = new TaskService(db, fileStorage, streakService);

        var updated = await taskService.UpdateRepositoryAsync(repo.Id, "Updated Repo", "New Description", true);

        updated.Title.Should().Be("Updated Repo");
        updated.Description.Should().Be("New Description");
        updated.IsStreakEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GlobalSearch_ShouldFindMatchingItems_AcrossAllHierarchiesAndAttachments()
    {
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository { Title = "Backend Repo", Description = "Architecture documentation" };
        var proj = new Project { Repository = repo, Title = "API Gateway", Description = "Microservice proxy" };
        var headTask = new HeadTask { Project = proj, Title = "Setup OAuth2", ReviewNotes = "Completed with JWT" };
        var subTask = new SubTask { HeadTask = headTask, Title = "Refresh token endpoint" };
        var attachment = new Attachment
        {
            HeadTask = headTask,
            OriginalFileName = "jwt_specification.pdf",
            RelativePath = "jwt_specification.pdf",
            FileSizeBytes = 2048,
            UploadedDate = DateTime.UtcNow
        };

        db.Repositories.Add(repo);
        db.Projects.Add(proj);
        db.HeadTasks.Add(headTask);
        db.SubTasks.Add(subTask);
        db.Attachments.Add(attachment);
        await db.SaveChangesAsync();

        var streakService = new StreakService(db);
        var fileStorage = new FileStorageService("test_attachments");
        var taskService = new TaskService(db, fileStorage, streakService);

        // Search 1: by term "Backend" -> finds Repository
        var res1 = await taskService.GlobalSearchAsync("Backend");
        res1.Should().ContainSingle(x => x.Type == SearchResultType.Repository && x.Title == "Backend Repo");

        // Search 2: by term "Architecture" in repo description
        var res2 = await taskService.GlobalSearchAsync("architecture");
        res2.Should().ContainSingle(x => x.Type == SearchResultType.Repository);

        // Search 3: by term "Gateway" -> finds Project
        var res3 = await taskService.GlobalSearchAsync("Gateway");
        res3.Should().ContainSingle(x => x.Type == SearchResultType.Project && x.Title == "API Gateway");

        // Search 4: by term "JWT" in ReviewNotes -> finds HeadTask
        var res4 = await taskService.GlobalSearchAsync("jwt");
        res4.Should().Contain(x => x.Type == SearchResultType.HeadTask && x.Title == "Setup OAuth2");
        res4.Should().Contain(x => x.Type == SearchResultType.Attachment && x.Title == "jwt_specification.pdf");

        // Search 5: by term "Refresh token" -> finds SubTask
        var res5 = await taskService.GlobalSearchAsync("refresh token");
        res5.Should().ContainSingle(x => x.Type == SearchResultType.SubTask && x.Title == "Refresh token endpoint");

        // Search 6: by attachment file name
        var res6 = await taskService.GlobalSearchAsync(".pdf");
        res6.Should().ContainSingle(x => x.Type == SearchResultType.Attachment && x.Title == "jwt_specification.pdf");
    }

    [Fact]
    public async Task DeleteHeadTask_ShouldCleanUpActivityLogs_AndRecalculateStreak()
    {
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository { Title = "Streak Cleanup Repo", IsStreakEnabled = true };
        var proj = new Project { Repository = repo, Title = "Streak Proj" };
        var task = new HeadTask { Project = proj, Title = "Active Task" };

        db.Repositories.Add(repo);
        db.Projects.Add(proj);
        db.HeadTasks.Add(task);
        await db.SaveChangesAsync();

        var streakService = new StreakService(db);
        var fileStorage = new FileStorageService("test_attachments");
        var taskService = new TaskService(db, fileStorage, streakService);

        // Mark task as done
        await taskService.UpdateHeadTaskStatusAsync(task.Id, ItemStatus.Done);
        var repoAfterDone = await db.Repositories.FindAsync(repo.Id);
        repoAfterDone!.CurrentStreak.Should().Be(1);

        var logsCount = await db.ActivityLogs.CountAsync(l => l.HeadTaskId == task.Id);
        logsCount.Should().Be(1);

        // Delete task
        await taskService.DeleteHeadTaskAsync(task.Id);

        var remainingLogs = await db.ActivityLogs.CountAsync(l => l.HeadTaskId == task.Id);
        remainingLogs.Should().Be(0);

        var repoAfterDelete = await db.Repositories.FindAsync(repo.Id);
        repoAfterDelete!.CurrentStreak.Should().Be(0);
        repoAfterDelete.LastActiveDate.Should().BeNull();
    }
}
