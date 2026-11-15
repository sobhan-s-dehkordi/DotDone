using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DotDone.Core.Enums;
using DotDone.Core.Models;

namespace DotDone.UI.ViewModels;

public partial class RepositoryViewModel : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _isStreakEnabled;

    [ObservableProperty]
    private int _currentStreak;

    [ObservableProperty]
    private DateOnly? _lastActiveDate;

    public ObservableCollection<ProjectViewModel> Projects { get; } = new();

    public int TotalProjectsCount => Projects.Count;
    public int TotalHeadTasksCount => Projects.Sum(p => p.HeadTasks.Count);
    public int TotalSubTasksCount => Projects.Sum(p => p.HeadTasks.Sum(t => t.SubTasks.Count));
    public int TotalTasksCount => TotalHeadTasksCount + TotalSubTasksCount;

    public string ProjectsSummary => $"{Projects.Count} " +
        Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("RepoProjectsCount");

    public static RepositoryViewModel FromModel(Repository model)
    {
        var vm = new RepositoryViewModel
        {
            Id = model.Id,
            Title = model.Title,
            Description = model.Description,
            IsStreakEnabled = model.IsStreakEnabled,
            CurrentStreak = model.CurrentStreak,
            LastActiveDate = model.LastActiveDate
        };

        foreach (var p in model.Projects)
        {
            vm.Projects.Add(ProjectViewModel.FromModel(p));
        }

        return vm;
    }
}

public partial class ProjectViewModel : ObservableObject
{
    public int Id { get; set; }
    public int RepositoryId { get; set; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private ItemStatus _status;

    [ObservableProperty]
    private string? _reviewNotes;

    [ObservableProperty]
    private DateTime _createdDate;

    [ObservableProperty]
    private DateTime? _modifiedDate;

    public ObservableCollection<HeadTaskViewModel> HeadTasks { get; } = new();
    public ObservableCollection<AttachmentViewModel> Attachments { get; } = new();

    public string L_ViewTask => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("ViewTask");
    public string L_EditTask => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("EditTask");
    public string L_DeleteTask => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("DeleteTask");

    public void NotifyLanguageChanged()
    {
        OnPropertyChanged(nameof(L_ViewTask));
        OnPropertyChanged(nameof(L_EditTask));
        OnPropertyChanged(nameof(L_DeleteTask));
        foreach (var task in HeadTasks)
        {
            task.NotifyLanguageChanged();
        }
    }

    public int TotalHeadTasksCount => HeadTasks.Count;
    public int DoneHeadTasksCount => HeadTasks.Count(t => t.Status == ItemStatus.Done);
    public int InProgressHeadTasksCount => HeadTasks.Count(t => t.Status == ItemStatus.InProgress);
    public int TodoHeadTasksCount => HeadTasks.Count(t => t.Status == ItemStatus.Todo);
    public int TotalSubTasksCount => HeadTasks.Sum(t => t.SubTasks.Count);
    public int TotalTasksCount => TotalHeadTasksCount + TotalSubTasksCount;

    public static ProjectViewModel FromModel(Project model)
    {
        var vm = new ProjectViewModel
        {
            Id = model.Id,
            RepositoryId = model.RepositoryId,
            Title = model.Title,
            Description = model.Description,
            Status = model.Status,
            ReviewNotes = model.ReviewNotes,
            CreatedDate = model.CreatedDate,
            ModifiedDate = model.ModifiedDate
        };

        foreach (var t in model.HeadTasks)
        {
            vm.HeadTasks.Add(HeadTaskViewModel.FromModel(t));
        }

        foreach (var a in model.Attachments)
        {
            vm.Attachments.Add(AttachmentViewModel.FromModel(a));
        }

        return vm;
    }
}

public partial class HeadTaskViewModel : ObservableObject
{
    public int Id { get; set; }
    public int ProjectId { get; set; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private ItemStatus _status;

    [ObservableProperty]
    private string? _reviewNotes;

    [ObservableProperty]
    private DateTime _createdDate;

    [ObservableProperty]
    private DateTime? _modifiedDate;

    [ObservableProperty]
    private bool _isExpanded;

    public ObservableCollection<SubTaskViewModel> SubTasks { get; } = new();
    public ObservableCollection<AttachmentViewModel> Attachments { get; } = new();

    public string L_ViewTask => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("ViewTask");
    public string L_EditTask => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("EditTask");
    public string L_DeleteTask => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("DeleteTask");

    public int CompletedSubTasksCount => SubTasks.Count(s => s.Status == ItemStatus.Done);
    public int TotalSubTasksCount => SubTasks.Count;
    public string ProgressSummary => TotalSubTasksCount > 0 ? $"{CompletedSubTasksCount}/{TotalSubTasksCount}" : "";

    public string L_MenuDetails => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("MenuDetails");
    public string L_MenuTransfer => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("MenuTransfer");
    public string L_MenuDelete => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("MenuDelete");
    public string L_QuickAddSubTaskPlaceholder => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("QuickAddSubTaskPlaceholder");
    public string L_TransferTaskTooltip => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("TransferTaskTooltip");

    public void NotifyProgressChanged()
    {
        OnPropertyChanged(nameof(CompletedSubTasksCount));
        OnPropertyChanged(nameof(TotalSubTasksCount));
        OnPropertyChanged(nameof(ProgressSummary));
    }

    public void NotifyLanguageChanged()
    {
        OnPropertyChanged(nameof(L_MenuDetails));
        OnPropertyChanged(nameof(L_MenuTransfer));
        OnPropertyChanged(nameof(L_MenuDelete));
        OnPropertyChanged(nameof(L_QuickAddSubTaskPlaceholder));
        OnPropertyChanged(nameof(L_TransferTaskTooltip));
        OnPropertyChanged(nameof(L_ViewTask));
        OnPropertyChanged(nameof(L_EditTask));
        OnPropertyChanged(nameof(L_DeleteTask));
        foreach (var sub in SubTasks)
        {
            sub.NotifyLanguageChanged();
        }
    }

    public static HeadTaskViewModel FromModel(HeadTask model)
    {
        var vm = new HeadTaskViewModel
        {
            Id = model.Id,
            ProjectId = model.ProjectId,
            Title = model.Title,
            Description = model.Description,
            Status = model.Status,
            ReviewNotes = model.ReviewNotes,
            CreatedDate = model.CreatedDate,
            ModifiedDate = model.ModifiedDate
        };

        foreach (var s in model.SubTasks)
        {
            vm.SubTasks.Add(SubTaskViewModel.FromModel(s));
        }

        foreach (var a in model.Attachments)
        {
            vm.Attachments.Add(AttachmentViewModel.FromModel(a));
        }

        return vm;
    }
}

public partial class SubTaskViewModel : ObservableObject
{
    public int Id { get; set; }
    public int HeadTaskId { get; set; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private ItemStatus _status;

    [ObservableProperty]
    private string? _reviewNotes;

    [ObservableProperty]
    private DateTime _createdDate;

    [ObservableProperty]
    private DateTime? _modifiedDate;

    public ObservableCollection<AttachmentViewModel> Attachments { get; } = new();

    public string L_ViewTask => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("ViewTask");
    public string L_EditTask => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("EditTask");
    public string L_DeleteTask => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("DeleteTask");

    public void NotifyLanguageChanged()
    {
        OnPropertyChanged(nameof(L_ViewTask));
        OnPropertyChanged(nameof(L_EditTask));
        OnPropertyChanged(nameof(L_DeleteTask));
    }

    public static SubTaskViewModel FromModel(SubTask model)
    {
        var vm = new SubTaskViewModel
        {
            Id = model.Id,
            HeadTaskId = model.HeadTaskId,
            Title = model.Title,
            Description = model.Description,
            Status = model.Status,
            ReviewNotes = model.ReviewNotes,
            CreatedDate = model.CreatedDate,
            ModifiedDate = model.ModifiedDate
        };

        foreach (var a in model.Attachments)
        {
            vm.Attachments.Add(AttachmentViewModel.FromModel(a));
        }

        return vm;
    }
}

public partial class AttachmentViewModel : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _originalFileName = string.Empty;

    [ObservableProperty]
    private string _relativePath = string.Empty;

    [ObservableProperty]
    private long _fileSizeBytes;

    [ObservableProperty]
    private DateTime _uploadedDate;

    public string FormattedSize
    {
        get
        {
            if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:F1} KB";
            return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
        }
    }

    public string L_OpenFile => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("OpenFile");
    public string L_Delete => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("Delete");
    public string L_DeleteAttachmentConfirmMessage => Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<DotDone.Services.ILocalizationService>(App.Services).GetString("DeleteAttachmentConfirmMessage");

    public static AttachmentViewModel FromModel(Attachment model)
    {
        return new AttachmentViewModel
        {
            Id = model.Id,
            OriginalFileName = model.OriginalFileName,
            RelativePath = model.RelativePath,
            FileSizeBytes = model.FileSizeBytes,
            UploadedDate = model.UploadedDate
        };
    }
}
