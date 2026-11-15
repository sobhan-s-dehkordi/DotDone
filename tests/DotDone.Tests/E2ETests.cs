using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DotDone.Core.Enums;
using DotDone.Core.Models;
using DotDone.Data;
using DotDone.Data.Context;
using DotDone.Data.Services;
using DotDone.Services;
using Xunit;

namespace DotDone.Tests;

public class E2ETests
{
    [Fact]
    public async Task CompleteE2E_LifecycleTest()
    {
        // 1. Setup real SQLite database file in a temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), $"DotDone_E2E_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "dotdone_test.db");

        try
        {
            var options = new DbContextOptionsBuilder<DotDoneDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            // 2. Initialize and Seed
            using (var db = new DotDoneDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                DbInitializer.Initialize(db);

                // Verify Initial Seeds
                db.Repositories.Should().HaveCount(1);
                var repo = await db.Repositories.Include(r => r.Projects).FirstAsync();
                repo.Title.Should().Be("کارهای شخصی و روزمره");
                repo.IsStreakEnabled.Should().BeTrue();
                repo.CurrentStreak.Should().Be(3);

                repo.Projects.Should().HaveCount(1);
                var proj = repo.Projects.First();
                proj.Title.Should().Be("توسعه و راه‌اندازی دات‌دان (DotDone)");

                var tasks = await db.HeadTasks.Include(t => t.SubTasks).Where(t => t.ProjectId == proj.Id).ToListAsync();
                tasks.Should().HaveCount(3);
                tasks.SelectMany(t => t.SubTasks).Should().HaveCount(6);

                var logs = await db.ActivityLogs.ToListAsync();
                logs.Should().HaveCount(5);
            }

            // 3. E2E Services Integration
            using (var db = new DotDoneDbContext(options))
            {
                var fileStorage = new FileStorageService(Path.Combine(tempDir, "Attachments"));
                var streakService = new StreakService(db);
                var heatmapService = new ActivityHeatmapService(db);
                var transferService = new TaskTransferService(db);
                var localization = new LocalizationService();
                var taskService = new TaskService(db, fileStorage, streakService);

                // Verify Heatmap summarizes initial seed data
                var heatmap = await heatmapService.GetActivityHeatmapAsync();
                heatmap.TotalActiveDays.Should().BeGreaterThan(0);
                heatmap.TotalWeight.Should().BeGreaterThan(0);

                // Create a second project
                var repo = await db.Repositories.FirstAsync();
                var proj2 = await taskService.CreateProjectAsync(repo.Id, "پروژه دوم تست");
                proj2.Should().NotBeNull();
                proj2.Id.Should().BeGreaterThan(0);

                // Create a new task in project 2
                var newTask = await taskService.CreateHeadTaskAsync(proj2.Id, "تسک تست عملکردی", "توضیحات کوتاه");
                newTask.Status.Should().Be(ItemStatus.Todo);

                // Attach a dummy file
                var dummyFile = Path.Combine(tempDir, "sample.txt");
                await File.WriteAllTextAsync(dummyFile, "DotDone E2E Test Attachment Content");
                var att = await taskService.AddAttachmentToHeadTaskAsync(newTask.Id, dummyFile);
                att.OriginalFileName.Should().Be("sample.txt");
                File.Exists(fileStorage.GetFullPath(att.RelativePath)).Should().BeTrue();

                // Advance status to InProgress -> checks streak & activity
                var updatedTask = await taskService.UpdateHeadTaskStatusAsync(newTask.Id, ItemStatus.InProgress);
                updatedTask.Status.Should().Be(ItemStatus.InProgress);

                // Advance status to Done -> checks streak & activity weight 2
                updatedTask = await taskService.UpdateHeadTaskStatusAsync(newTask.Id, ItemStatus.Done);
                updatedTask.Status.Should().Be(ItemStatus.Done);

                // Deep Copy task to Project 1
                var proj1 = await db.Projects.FirstAsync(p => p.Id != proj2.Id);
                var copiedTask = await transferService.CopyHeadTaskAsync(newTask.Id, proj1.Id, resetStatusToTodo: true);
                copiedTask.ProjectId.Should().Be(proj1.Id);
                copiedTask.Status.Should().Be(ItemStatus.Todo);
                copiedTask.Attachments.Should().HaveCount(1);

                // Test Localization formatting
                var todayShamsi = localization.FormatDate(DateOnly.FromDateTime(DateTime.Now));
                todayShamsi.Should().NotBeNullOrWhiteSpace();
                localization.ToLocalDigits("12345").Should().Be("۱۲۳۴۵");
            }
        }
        finally
        {
            // Cleanup temp test directory
            try
            {
                SqliteConnection.ClearAllPools();
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch { }
        }
    }
}
