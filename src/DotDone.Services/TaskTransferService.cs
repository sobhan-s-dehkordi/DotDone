using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DotDone.Core.Enums;
using DotDone.Core.Models;
using DotDone.Data.Context;

namespace DotDone.Services;

public interface ITaskTransferService
{
    Task<HeadTask> CopyHeadTaskAsync(int headTaskId, int targetProjectId, bool resetStatusToTodo = true);
    Task<HeadTask> MoveHeadTaskAsync(int headTaskId, int targetProjectId);
    Task<SubTask> CopySubTaskAsync(int subTaskId, int targetHeadTaskId, bool resetStatusToTodo = true);
    Task<SubTask> MoveSubTaskAsync(int subTaskId, int targetHeadTaskId);
}

public class TaskTransferService : ITaskTransferService
{
    private readonly DotDoneDbContext _db;

    public TaskTransferService(DotDoneDbContext db)
    {
        _db = db;
    }

    public async Task<HeadTask> CopyHeadTaskAsync(int headTaskId, int targetProjectId, bool resetStatusToTodo = true)
    {
        var targetProject = await _db.Projects.FindAsync(targetProjectId)
            ?? throw new ArgumentException($"Target project with ID {targetProjectId} not found.");

        var originalTask = await _db.HeadTasks
            .Include(t => t.SubTasks)
                .ThenInclude(s => s.Attachments)
            .Include(t => t.Attachments)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == headTaskId)
            ?? throw new ArgumentException($"HeadTask with ID {headTaskId} not found.");

        var newTask = new HeadTask
        {
            ProjectId = targetProjectId,
            Title = originalTask.Title,
            Description = originalTask.Description,
            Status = resetStatusToTodo ? ItemStatus.Todo : originalTask.Status,
            ReviewNotes = resetStatusToTodo ? null : originalTask.ReviewNotes,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = null,
            Attachments = originalTask.Attachments.Select(a => new Attachment
            {
                OriginalFileName = a.OriginalFileName,
                StoredFileName = a.StoredFileName,
                RelativePath = a.RelativePath,
                FileSizeBytes = a.FileSizeBytes,
                UploadedDate = DateTime.UtcNow
            }).ToList(),
            SubTasks = originalTask.SubTasks.Select(s => new SubTask
            {
                Title = s.Title,
                Description = s.Description,
                Status = resetStatusToTodo ? ItemStatus.Todo : s.Status,
                ReviewNotes = resetStatusToTodo ? null : s.ReviewNotes,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = null,
                Attachments = s.Attachments.Select(sa => new Attachment
                {
                    OriginalFileName = sa.OriginalFileName,
                    StoredFileName = sa.StoredFileName,
                    RelativePath = sa.RelativePath,
                    FileSizeBytes = sa.FileSizeBytes,
                    UploadedDate = DateTime.UtcNow
                }).ToList()
            }).ToList()
        };

        _db.HeadTasks.Add(newTask);
        await _db.SaveChangesAsync();

        return newTask;
    }

    public async Task<HeadTask> MoveHeadTaskAsync(int headTaskId, int targetProjectId)
    {
        var targetProject = await _db.Projects.FindAsync(targetProjectId)
            ?? throw new ArgumentException($"Target project with ID {targetProjectId} not found.");

        var task = await _db.HeadTasks.FindAsync(headTaskId)
            ?? throw new ArgumentException($"HeadTask with ID {headTaskId} not found.");

        task.ProjectId = targetProjectId;
        task.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<SubTask> CopySubTaskAsync(int subTaskId, int targetHeadTaskId, bool resetStatusToTodo = true)
    {
        var targetHeadTask = await _db.HeadTasks.FindAsync(targetHeadTaskId)
            ?? throw new ArgumentException($"Target HeadTask with ID {targetHeadTaskId} not found.");

        var originalSub = await _db.SubTasks
            .Include(s => s.Attachments)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == subTaskId)
            ?? throw new ArgumentException($"SubTask with ID {subTaskId} not found.");

        var newSub = new SubTask
        {
            HeadTaskId = targetHeadTaskId,
            Title = originalSub.Title,
            Description = originalSub.Description,
            Status = resetStatusToTodo ? ItemStatus.Todo : originalSub.Status,
            ReviewNotes = resetStatusToTodo ? null : originalSub.ReviewNotes,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = null,
            Attachments = originalSub.Attachments.Select(a => new Attachment
            {
                OriginalFileName = a.OriginalFileName,
                StoredFileName = a.StoredFileName,
                RelativePath = a.RelativePath,
                FileSizeBytes = a.FileSizeBytes,
                UploadedDate = DateTime.UtcNow
            }).ToList()
        };

        _db.SubTasks.Add(newSub);
        await _db.SaveChangesAsync();

        return newSub;
    }

    public async Task<SubTask> MoveSubTaskAsync(int subTaskId, int targetHeadTaskId)
    {
        var targetHeadTask = await _db.HeadTasks.FindAsync(targetHeadTaskId)
            ?? throw new ArgumentException($"Target HeadTask with ID {targetHeadTaskId} not found.");

        var sub = await _db.SubTasks.FindAsync(subTaskId)
            ?? throw new ArgumentException($"SubTask with ID {subTaskId} not found.");

        sub.HeadTaskId = targetHeadTaskId;
        sub.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return sub;
    }
}
