using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DotDone.Core.Enums;
using DotDone.Core.Models;
using DotDone.Data.Context;
using DotDone.Data.Services;

namespace DotDone.Services;

public interface ITaskService
{
    // Repository
    Task<List<Repository>> GetAllRepositoriesAsync();
    Task<Repository?> GetRepositoryByIdAsync(int id);
    Task<Repository> CreateRepositoryAsync(string title, string? description = null, bool isStreakEnabled = false);
    Task<Repository> UpdateRepositoryAsync(int id, string title, string? description = null, bool? isStreakEnabled = null);
    Task DeleteRepositoryAsync(int id);

    // Project
    Task<List<Project>> GetProjectsByRepositoryIdAsync(int repositoryId);
    Task<Project?> GetProjectByIdAsync(int id);
    Task<Project> CreateProjectAsync(int repositoryId, string title, string? description = null);
    Task<Project> UpdateProjectAsync(int id, string title, string? description = null, string? reviewNotes = null);
    Task<Project> UpdateProjectStatusAsync(int id, ItemStatus status);
    Task DeleteProjectAsync(int id);

    // HeadTask
    Task<List<HeadTask>> GetHeadTasksByProjectIdAsync(int projectId);
    Task<HeadTask?> GetHeadTaskByIdAsync(int id);
    Task<HeadTask> CreateHeadTaskAsync(int projectId, string title, string? description = null);
    Task<HeadTask> UpdateHeadTaskAsync(int id, string title, string? description = null, string? reviewNotes = null);
    Task<HeadTask> UpdateHeadTaskStatusAsync(int id, ItemStatus status);
    Task DeleteHeadTaskAsync(int id);

    // SubTask
    Task<List<SubTask>> GetSubTasksByHeadTaskIdAsync(int headTaskId);
    Task<SubTask?> GetSubTaskByIdAsync(int id);
    Task<SubTask> CreateSubTaskAsync(int headTaskId, string title, string? description = null);
    Task<SubTask> UpdateSubTaskAsync(int id, string title, string? description = null, string? reviewNotes = null);
    Task<SubTask> UpdateSubTaskStatusAsync(int id, ItemStatus status);
    Task DeleteSubTaskAsync(int id);

    // Attachments
    Task<Attachment> AddAttachmentToProjectAsync(int projectId, string sourceFilePath);
    Task<Attachment> AddAttachmentToHeadTaskAsync(int headTaskId, string sourceFilePath);
    Task<Attachment> AddAttachmentToSubTaskAsync(int subTaskId, string sourceFilePath);
    Task DeleteAttachmentAsync(int attachmentId);

    // Global Search
    Task<List<SearchResultItemDto>> GlobalSearchAsync(string query);
}

public class TaskService : ITaskService
{
    private readonly DotDoneDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly IStreakService _streakService;

    public TaskService(DotDoneDbContext db, IFileStorageService fileStorage, IStreakService streakService)
    {
        _db = db;
        _fileStorage = fileStorage;
        _streakService = streakService;
    }

    #region Repository
    public async Task<List<Repository>> GetAllRepositoriesAsync()
    {
        return await _db.Repositories
            .Include(r => r.Projects)
                .ThenInclude(p => p.HeadTasks)
                    .ThenInclude(t => t.SubTasks)
            .OrderBy(r => r.Title)
            .ToListAsync();
    }

    public async Task<Repository?> GetRepositoryByIdAsync(int id)
    {
        return await _db.Repositories
            .Include(r => r.Projects)
                .ThenInclude(p => p.HeadTasks)
                    .ThenInclude(t => t.SubTasks)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Repository> CreateRepositoryAsync(string title, string? description = null, bool isStreakEnabled = false)
    {
        var repo = new Repository
        {
            Title = title.Trim(),
            Description = description?.Trim(),
            IsStreakEnabled = isStreakEnabled,
            CreatedDate = DateTime.UtcNow
        };
        _db.Repositories.Add(repo);
        await _db.SaveChangesAsync();
        return repo;
    }

    public async Task<Repository> UpdateRepositoryAsync(int id, string title, string? description = null, bool? isStreakEnabled = null)
    {
        var repo = await _db.Repositories.FindAsync(id)
            ?? throw new ArgumentException($"Repository {id} not found.");

        repo.Title = title.Trim();
        repo.Description = description?.Trim();
        if (isStreakEnabled.HasValue)
        {
            repo.IsStreakEnabled = isStreakEnabled.Value;
        }

        await _db.SaveChangesAsync();
        return repo;
    }

    public async Task DeleteRepositoryAsync(int id)
    {
        var repo = await _db.Repositories.FindAsync(id);
        if (repo != null)
        {
            var logs = await _db.ActivityLogs.Where(l => l.RepositoryId == id).ToListAsync();
            if (logs.Count > 0)
            {
                _db.ActivityLogs.RemoveRange(logs);
            }
            _db.Repositories.Remove(repo);
            await _db.SaveChangesAsync();
        }
    }
    #endregion

    #region Project
    public async Task<List<Project>> GetProjectsByRepositoryIdAsync(int repositoryId)
    {
        return await _db.Projects
            .Where(p => p.RepositoryId == repositoryId)
            .Include(p => p.HeadTasks)
                .ThenInclude(t => t.SubTasks)
            .Include(p => p.Attachments)
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        return await _db.Projects
            .Include(p => p.Repository)
            .Include(p => p.HeadTasks)
                .ThenInclude(t => t.SubTasks)
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project> CreateProjectAsync(int repositoryId, string title, string? description = null)
    {
        var project = new Project
        {
            RepositoryId = repositoryId,
            Title = title.Trim(),
            Description = Truncate500(description),
            CreatedDate = DateTime.UtcNow
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    public async Task<Project> UpdateProjectAsync(int id, string title, string? description = null, string? reviewNotes = null)
    {
        var project = await _db.Projects.FindAsync(id)
            ?? throw new ArgumentException($"Project {id} not found.");

        project.Title = title.Trim();
        project.Description = Truncate500(description);
        project.ReviewNotes = Truncate500(reviewNotes);
        project.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return project;
    }

    public async Task<Project> UpdateProjectStatusAsync(int id, ItemStatus status)
    {
        var project = await _db.Projects.Include(p => p.Repository).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new ArgumentException($"Project {id} not found.");

        project.Status = status;
        project.ModifiedDate = DateTime.UtcNow;

        await RecordActivityAsync(project.RepositoryId, project.Id, null, null, status);
        await _db.SaveChangesAsync();
        return project;
    }

    public async Task DeleteProjectAsync(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project != null)
        {
            var repoId = project.RepositoryId;
            var logs = await _db.ActivityLogs.Where(l => l.ProjectId == id).ToListAsync();
            if (logs.Count > 0)
            {
                _db.ActivityLogs.RemoveRange(logs);
            }
            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();

            if (repoId > 0)
            {
                await _streakService.RecalculateStreakAsync(repoId);
            }
        }
    }
    #endregion

    #region HeadTask
    public async Task<List<HeadTask>> GetHeadTasksByProjectIdAsync(int projectId)
    {
        return await _db.HeadTasks
            .Where(t => t.ProjectId == projectId)
            .Include(t => t.SubTasks)
            .Include(t => t.Attachments)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();
    }

    public async Task<HeadTask?> GetHeadTaskByIdAsync(int id)
    {
        return await _db.HeadTasks
            .Include(t => t.Project)
            .Include(t => t.SubTasks)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<HeadTask> CreateHeadTaskAsync(int projectId, string title, string? description = null)
    {
        var headTask = new HeadTask
        {
            ProjectId = projectId,
            Title = title.Trim(),
            Description = Truncate500(description),
            CreatedDate = DateTime.UtcNow
        };
        _db.HeadTasks.Add(headTask);
        await _db.SaveChangesAsync();
        return headTask;
    }

    public async Task<HeadTask> UpdateHeadTaskAsync(int id, string title, string? description = null, string? reviewNotes = null)
    {
        var task = await _db.HeadTasks.FindAsync(id)
            ?? throw new ArgumentException($"HeadTask {id} not found.");

        task.Title = title.Trim();
        task.Description = Truncate500(description);
        task.ReviewNotes = Truncate500(reviewNotes);
        task.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<HeadTask> UpdateHeadTaskStatusAsync(int id, ItemStatus status)
    {
        var task = await _db.HeadTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new ArgumentException($"HeadTask {id} not found.");

        task.Status = status;
        task.ModifiedDate = DateTime.UtcNow;

        await RecordActivityAsync(task.Project?.RepositoryId ?? 0, task.ProjectId, task.Id, null, status);
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task DeleteHeadTaskAsync(int id)
    {
        var task = await _db.HeadTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id);
        if (task != null)
        {
            var repoId = task.Project?.RepositoryId ?? 0;
            var logs = await _db.ActivityLogs.Where(l => l.HeadTaskId == id).ToListAsync();
            if (logs.Count > 0)
            {
                _db.ActivityLogs.RemoveRange(logs);
            }
            _db.HeadTasks.Remove(task);
            await _db.SaveChangesAsync();

            if (repoId > 0)
            {
                await _streakService.RecalculateStreakAsync(repoId);
            }
        }
    }
    #endregion

    #region SubTask
    public async Task<List<SubTask>> GetSubTasksByHeadTaskIdAsync(int headTaskId)
    {
        return await _db.SubTasks
            .Where(s => s.HeadTaskId == headTaskId)
            .Include(s => s.Attachments)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync();
    }

    public async Task<SubTask?> GetSubTaskByIdAsync(int id)
    {
        return await _db.SubTasks
            .Include(s => s.HeadTask)
                .ThenInclude(t => t!.Project)
            .Include(s => s.Attachments)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<SubTask> CreateSubTaskAsync(int headTaskId, string title, string? description = null)
    {
        var subTask = new SubTask
        {
            HeadTaskId = headTaskId,
            Title = title.Trim(),
            Description = Truncate500(description),
            CreatedDate = DateTime.UtcNow
        };
        _db.SubTasks.Add(subTask);
        await _db.SaveChangesAsync();
        return subTask;
    }

    public async Task<SubTask> UpdateSubTaskAsync(int id, string title, string? description = null, string? reviewNotes = null)
    {
        var sub = await _db.SubTasks.FindAsync(id)
            ?? throw new ArgumentException($"SubTask {id} not found.");

        sub.Title = title.Trim();
        sub.Description = Truncate500(description);
        sub.ReviewNotes = Truncate500(reviewNotes);
        sub.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return sub;
    }

    public async Task<SubTask> UpdateSubTaskStatusAsync(int id, ItemStatus status)
    {
        var sub = await _db.SubTasks
            .Include(s => s.HeadTask)
                .ThenInclude(t => t!.Project)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new ArgumentException($"SubTask {id} not found.");

        sub.Status = status;
        sub.ModifiedDate = DateTime.UtcNow;

        var repoId = sub.HeadTask?.Project?.RepositoryId ?? 0;
        var projId = sub.HeadTask?.ProjectId ?? 0;
        await RecordActivityAsync(repoId, projId, sub.HeadTaskId, sub.Id, status);
        await _db.SaveChangesAsync();
        return sub;
    }

    public async Task DeleteSubTaskAsync(int id)
    {
        var sub = await _db.SubTasks.Include(s => s.HeadTask).ThenInclude(t => t!.Project).FirstOrDefaultAsync(s => s.Id == id);
        if (sub != null)
        {
            var repoId = sub.HeadTask?.Project?.RepositoryId ?? 0;
            var logs = await _db.ActivityLogs.Where(l => l.SubTaskId == id).ToListAsync();
            if (logs.Count > 0)
            {
                _db.ActivityLogs.RemoveRange(logs);
            }
            _db.SubTasks.Remove(sub);
            await _db.SaveChangesAsync();

            if (repoId > 0)
            {
                await _streakService.RecalculateStreakAsync(repoId);
            }
        }
    }
    #endregion

    #region Attachments
    public async Task<Attachment> AddAttachmentToProjectAsync(int projectId, string sourceFilePath)
    {
        var saved = await _fileStorage.SaveFileAsync(sourceFilePath);
        var attachment = new Attachment
        {
            ProjectId = projectId,
            OriginalFileName = System.IO.Path.GetFileName(sourceFilePath),
            StoredFileName = saved.storedFileName,
            RelativePath = saved.relativePath,
            FileSizeBytes = saved.fileSizeBytes,
            UploadedDate = DateTime.UtcNow
        };
        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync();
        return attachment;
    }

    public async Task<Attachment> AddAttachmentToHeadTaskAsync(int headTaskId, string sourceFilePath)
    {
        var saved = await _fileStorage.SaveFileAsync(sourceFilePath);
        var attachment = new Attachment
        {
            HeadTaskId = headTaskId,
            OriginalFileName = System.IO.Path.GetFileName(sourceFilePath),
            StoredFileName = saved.storedFileName,
            RelativePath = saved.relativePath,
            FileSizeBytes = saved.fileSizeBytes,
            UploadedDate = DateTime.UtcNow
        };
        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync();
        return attachment;
    }

    public async Task<Attachment> AddAttachmentToSubTaskAsync(int subTaskId, string sourceFilePath)
    {
        var saved = await _fileStorage.SaveFileAsync(sourceFilePath);
        var attachment = new Attachment
        {
            SubTaskId = subTaskId,
            OriginalFileName = System.IO.Path.GetFileName(sourceFilePath),
            StoredFileName = saved.storedFileName,
            RelativePath = saved.relativePath,
            FileSizeBytes = saved.fileSizeBytes,
            UploadedDate = DateTime.UtcNow
        };
        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync();
        return attachment;
    }

    public async Task DeleteAttachmentAsync(int attachmentId)
    {
        var att = await _db.Attachments.FindAsync(attachmentId);
        if (att != null)
        {
            _fileStorage.DeleteFile(att.RelativePath);
            _db.Attachments.Remove(att);
            await _db.SaveChangesAsync();
        }
    }
    #endregion

    private async Task RecordActivityAsync(int repoId, int projectId, int? headTaskId, int? subTaskId, ItemStatus status)
    {
        var existingLogs = await _db.ActivityLogs
            .Where(l => subTaskId != null
                ? l.SubTaskId == subTaskId
                : headTaskId != null
                    ? l.HeadTaskId == headTaskId
                    : (l.ProjectId == projectId && l.HeadTaskId == null && l.SubTaskId == null))
            .ToListAsync();

        if (existingLogs.Count > 0)
        {
            _db.ActivityLogs.RemoveRange(existingLogs);
        }

        if (status == ItemStatus.InProgress || status == ItemStatus.Done)
        {
            int weight = status == ItemStatus.Done ? 2 : 1;
            var today = DateOnly.FromDateTime(DateTime.Now);

            _db.ActivityLogs.Add(new DailyActivityLog
            {
                ActivityDate = today,
                RepositoryId = repoId,
                ProjectId = projectId,
                HeadTaskId = headTaskId,
                SubTaskId = subTaskId,
                StatusChangedTo = status,
                ActivityWeight = weight,
                RecordedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        if (repoId > 0)
        {
            await _streakService.RecalculateStreakAsync(repoId);
        }
    }

    private static string? Truncate500(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }

    #region Global Search
    public async Task<List<SearchResultItemDto>> GlobalSearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<SearchResultItemDto>();

        query = query.Trim().ToLower();
        var results = new List<SearchResultItemDto>();

        // 1. Repositories
        var repos = await _db.Repositories
            .Where(r => r.Title.ToLower().Contains(query) || (r.Description != null && r.Description.ToLower().Contains(query)))
            .ToListAsync();

        foreach (var r in repos)
        {
            results.Add(new SearchResultItemDto
            {
                Id = r.Id,
                Type = SearchResultType.Repository,
                Title = r.Title,
                MatchedSnippet = r.Description ?? string.Empty,
                LocationBreadcrumb = r.Title,
                RepositoryId = r.Id
            });
        }

        // 2. Projects
        var projects = await _db.Projects
            .Include(p => p.Repository)
            .Where(p => p.Title.ToLower().Contains(query)
                     || (p.Description != null && p.Description.ToLower().Contains(query))
                     || (p.ReviewNotes != null && p.ReviewNotes.ToLower().Contains(query)))
            .ToListAsync();

        foreach (var p in projects)
        {
            results.Add(new SearchResultItemDto
            {
                Id = p.Id,
                Type = SearchResultType.Project,
                Title = p.Title,
                MatchedSnippet = !string.IsNullOrEmpty(p.Description) && p.Description.ToLower().Contains(query)
                    ? p.Description
                    : (p.ReviewNotes ?? string.Empty),
                LocationBreadcrumb = $"{p.Repository?.Title} > {p.Title}",
                RepositoryId = p.RepositoryId,
                ProjectId = p.Id,
                Status = p.Status
            });
        }

        // 3. HeadTasks
        var headTasks = await _db.HeadTasks
            .Include(t => t.Project)
                .ThenInclude(p => p!.Repository)
            .Where(t => t.Title.ToLower().Contains(query)
                     || (t.Description != null && t.Description.ToLower().Contains(query))
                     || (t.ReviewNotes != null && t.ReviewNotes.ToLower().Contains(query)))
            .ToListAsync();

        foreach (var t in headTasks)
        {
            results.Add(new SearchResultItemDto
            {
                Id = t.Id,
                Type = SearchResultType.HeadTask,
                Title = t.Title,
                MatchedSnippet = !string.IsNullOrEmpty(t.Description) && t.Description.ToLower().Contains(query)
                    ? t.Description
                    : (t.ReviewNotes ?? string.Empty),
                LocationBreadcrumb = $"{t.Project?.Repository?.Title} > {t.Project?.Title} > {t.Title}",
                RepositoryId = t.Project?.RepositoryId ?? 0,
                ProjectId = t.ProjectId,
                HeadTaskId = t.Id,
                Status = t.Status
            });
        }

        // 4. SubTasks
        var subTasks = await _db.SubTasks
            .Include(s => s.HeadTask)
                .ThenInclude(t => t!.Project)
                    .ThenInclude(p => p!.Repository)
            .Where(s => s.Title.ToLower().Contains(query)
                     || (s.Description != null && s.Description.ToLower().Contains(query))
                     || (s.ReviewNotes != null && s.ReviewNotes.ToLower().Contains(query)))
            .ToListAsync();

        foreach (var s in subTasks)
        {
            results.Add(new SearchResultItemDto
            {
                Id = s.Id,
                Type = SearchResultType.SubTask,
                Title = s.Title,
                MatchedSnippet = !string.IsNullOrEmpty(s.Description) && s.Description.ToLower().Contains(query)
                    ? s.Description
                    : (s.ReviewNotes ?? string.Empty),
                LocationBreadcrumb = $"{s.HeadTask?.Project?.Repository?.Title} > {s.HeadTask?.Project?.Title} > {s.HeadTask?.Title} > {s.Title}",
                RepositoryId = s.HeadTask?.Project?.RepositoryId ?? 0,
                ProjectId = s.HeadTask?.ProjectId,
                HeadTaskId = s.HeadTaskId,
                Status = s.Status
            });
        }

        // 5. Attachments
        var attachments = await _db.Attachments
            .Include(a => a.Project)
                .ThenInclude(p => p!.Repository)
            .Include(a => a.HeadTask)
                .ThenInclude(t => t!.Project)
                    .ThenInclude(p => p!.Repository)
            .Include(a => a.SubTask)
                .ThenInclude(s => s!.HeadTask)
                    .ThenInclude(t => t!.Project)
                        .ThenInclude(p => p!.Repository)
            .Where(a => a.OriginalFileName.ToLower().Contains(query))
            .ToListAsync();

        foreach (var a in attachments)
        {
            string breadcrumb = string.Empty;
            int repoId = 0;
            int? projId = null;
            int? taskId = null;

            if (a.Project != null)
            {
                breadcrumb = $"{a.Project.Repository?.Title} > {a.Project.Title} > [Attachment]";
                repoId = a.Project.RepositoryId;
                projId = a.ProjectId;
            }
            else if (a.HeadTask != null)
            {
                breadcrumb = $"{a.HeadTask.Project?.Repository?.Title} > {a.HeadTask.Project?.Title} > {a.HeadTask.Title} > [Attachment]";
                repoId = a.HeadTask.Project?.RepositoryId ?? 0;
                projId = a.HeadTask.ProjectId;
                taskId = a.HeadTaskId;
            }
            else if (a.SubTask != null)
            {
                breadcrumb = $"{a.SubTask.HeadTask?.Project?.Repository?.Title} > {a.SubTask.HeadTask?.Project?.Title} > {a.SubTask.HeadTask?.Title} > {a.SubTask.Title} > [Attachment]";
                repoId = a.SubTask.HeadTask?.Project?.RepositoryId ?? 0;
                projId = a.SubTask.HeadTask?.ProjectId;
                taskId = a.SubTask.HeadTaskId;
            }

            results.Add(new SearchResultItemDto
            {
                Id = a.Id,
                Type = SearchResultType.Attachment,
                Title = a.OriginalFileName,
                MatchedSnippet = $"Attachment ({a.FileSizeBytes / 1024} KB)",
                LocationBreadcrumb = breadcrumb,
                RepositoryId = repoId,
                ProjectId = projId,
                HeadTaskId = taskId
            });
        }

        return results;
    }
    #endregion
}
