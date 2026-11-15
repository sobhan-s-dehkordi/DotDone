using System;
using System.Threading.Tasks;
using FluentAssertions;
using DotDone.Core.Enums;
using DotDone.Core.Models;
using DotDone.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DotDone.Tests;

public class TaskTransferTests
{
    [Fact]
    public async Task CopyHeadTask_ShouldDeepCopy_TaskAndAllSubTasks()
    {
        // Arrange
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository { Title = "Main Repo" };
        var projA = new Project { Repository = repo, Title = "Project Alpha" };
        var projB = new Project { Repository = repo, Title = "Project Beta" };
        db.Repositories.Add(repo);
        db.Projects.AddRange(projA, projB);

        var originalTask = new HeadTask
        {
            Project = projA,
            Title = "Release Sprint 1",
            Description = "Prepare release assets and binaries",
            Status = ItemStatus.Done,
            ReviewNotes = "Shipped on time",
            Attachments =
            {
                new Attachment { OriginalFileName = "spec.pdf", StoredFileName = "abc.pdf", RelativePath = "p/abc.pdf", FileSizeBytes = 1024 }
            },
            SubTasks =
            {
                new SubTask { Title = "Build MSIX", Status = ItemStatus.Done, Description = "Sign package" },
                new SubTask { Title = "Upload Release Notes", Status = ItemStatus.Done }
            }
        };
        db.HeadTasks.Add(originalTask);
        await db.SaveChangesAsync();

        var transferService = new TaskTransferService(db);

        // Act: Copy HeadTask from Project Alpha to Project Beta
        var copiedTask = await transferService.CopyHeadTaskAsync(originalTask.Id, projB.Id, resetStatusToTodo: true);

        // Assert
        copiedTask.Should().NotBeNull();
        copiedTask.Id.Should().NotBe(originalTask.Id);
        copiedTask.ProjectId.Should().Be(projB.Id);
        copiedTask.Title.Should().Be("Release Sprint 1");
        copiedTask.Description.Should().Be("Prepare release assets and binaries");
        copiedTask.Status.Should().Be(ItemStatus.Todo); // Reset
        copiedTask.ReviewNotes.Should().BeNull(); // Cleared for fresh task
        copiedTask.Attachments.Should().HaveCount(1);
        copiedTask.Attachments[0].OriginalFileName.Should().Be("spec.pdf");

        // Check SubTasks
        copiedTask.SubTasks.Should().HaveCount(2);
        copiedTask.SubTasks[0].Title.Should().Be("Build MSIX");
        copiedTask.SubTasks[0].Status.Should().Be(ItemStatus.Todo);
        copiedTask.SubTasks[0].Description.Should().Be("Sign package");
        copiedTask.SubTasks[0].Id.Should().NotBe(originalTask.SubTasks[0].Id);

        // Verify original in Project A is intact
        var reloadedOriginal = await db.HeadTasks
            .Include(t => t.SubTasks)
            .FirstAsync(t => t.Id == originalTask.Id);
        reloadedOriginal.ProjectId.Should().Be(projA.Id);
        reloadedOriginal.Status.Should().Be(ItemStatus.Done);
        reloadedOriginal.SubTasks.Should().HaveCount(2);
    }

    [Fact]
    public async Task MoveHeadTask_ShouldChangeProjectId_WithoutCreatingDuplicates()
    {
        var (db, conn) = TestDbContextFactory.CreateInMemoryDatabase();
        using var _ = conn;
        using var __ = db;

        var repo = new Repository { Title = "Dev Repo" };
        var proj1 = new Project { Repository = repo, Title = "Source Project" };
        var proj2 = new Project { Repository = repo, Title = "Target Project" };
        db.Repositories.Add(repo);
        db.Projects.AddRange(proj1, proj2);

        var task = new HeadTask
        {
            Project = proj1,
            Title = "Task to Move",
            Status = ItemStatus.InProgress,
            SubTasks = { new SubTask { Title = "Child SubTask" } }
        };
        db.HeadTasks.Add(task);
        await db.SaveChangesAsync();

        var transferService = new TaskTransferService(db);

        // Act
        var moved = await transferService.MoveHeadTaskAsync(task.Id, proj2.Id);

        // Assert
        moved.ProjectId.Should().Be(proj2.Id);
        var totalTasks = await db.HeadTasks.CountAsync();
        totalTasks.Should().Be(1); // No duplicate created
    }
}
