using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotDone.Core.Enums;
using DotDone.Data.Services;
using DotDone.Services;

namespace DotDone.UI.ViewModels;

public enum ItemType
{
    Project,
    HeadTask,
    SubTask
}

public partial class DetailDialogViewModel : ObservableObject
{
    private readonly ITaskService _taskService;
    private readonly IFileStorageService _fileStorage;
    private readonly ILocalizationService _localization;

    public int ItemId { get; }
    public ItemType Type { get; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private ItemStatus _status;

    [ObservableProperty]
    private string _reviewNotes = string.Empty;

    [ObservableProperty]
    private string _formattedCreatedDate = string.Empty;

    [ObservableProperty]
    private string _formattedModifiedDate = string.Empty;

    public ObservableCollection<AttachmentViewModel> Attachments { get; } = new();

    public int DescriptionLength => Description?.Length ?? 0;
    public int ReviewNotesLength => ReviewNotes?.Length ?? 0;

    public string DescriptionCounter => $"{DescriptionLength} / 500";
    public string ReviewNotesCounter => $"{ReviewNotesLength} / 500";

    public Microsoft.UI.Xaml.FlowDirection FlowDirection => _localization.IsPersian
        ? Microsoft.UI.Xaml.FlowDirection.RightToLeft
        : Microsoft.UI.Xaml.FlowDirection.LeftToRight;

    public string DialogTitle => Type switch
    {
        ItemType.Project => _localization.GetString("ProjectDetailsTitle"),
        ItemType.HeadTask => _localization.GetString("TaskDetailsTitle"),
        ItemType.SubTask => _localization.GetString("SubTaskDetailsTitle"),
        _ => _localization.GetString("TaskDetailsTitle")
    };

    public string L_Title => _localization.GetString("Title");
    public string L_TitlePlaceholder => _localization.GetString("TitlePlaceholder");
    public string L_Status => _localization.GetString("Status");
    public string L_StatusTodo => _localization.GetString("StatusTodo");
    public string L_StatusInProgress => _localization.GetString("StatusInProgress");
    public string L_StatusDone => _localization.GetString("StatusDone");
    public string L_Description => _localization.GetString("Description");
    public string L_DescriptionPlaceholder => _localization.GetString("DescriptionPlaceholder");
    public string L_ReviewNotes => _localization.GetString("ReviewNotes");
    public string L_ReviewNotesPlaceholder => _localization.GetString("ReviewNotesPlaceholder");
    public string L_Attachments => _localization.GetString("Attachments");
    public string L_AddFile => _localization.GetString("AddFile");
    public string L_OpenFile => _localization.GetString("OpenFile");
    public string L_Delete => _localization.GetString("Delete");
    public string L_Save => _localization.GetString("Save");
    public string L_Cancel => _localization.GetString("Cancel");
    public string L_CreatedDateLabel => _localization.GetString("CreatedDateLabel");
    public string L_ModifiedDateLabel => _localization.GetString("ModifiedDateLabel");

    public DetailDialogViewModel(
        int itemId,
        ItemType type,
        string title,
        string? description,
        ItemStatus status,
        string? reviewNotes,
        DateTime createdDate,
        DateTime? modifiedDate,
        System.Collections.Generic.IEnumerable<AttachmentViewModel> attachments,
        ITaskService taskService,
        IFileStorageService fileStorage,
        ILocalizationService localization)
    {
        _taskService = taskService;
        _fileStorage = fileStorage;
        _localization = localization;

        ItemId = itemId;
        Type = type;
        Title = title;
        Description = description ?? string.Empty;
        Status = status;
        ReviewNotes = reviewNotes ?? string.Empty;

        FormattedCreatedDate = _localization.FormatDate(createdDate);
        FormattedModifiedDate = modifiedDate.HasValue ? _localization.FormatDate(modifiedDate.Value) : "-";

        foreach (var a in attachments)
        {
            Attachments.Add(a);
        }
    }

    partial void OnDescriptionChanged(string value)
    {
        OnPropertyChanged(nameof(DescriptionLength));
        OnPropertyChanged(nameof(DescriptionCounter));
    }

    partial void OnReviewNotesChanged(string value)
    {
        OnPropertyChanged(nameof(ReviewNotesLength));
        OnPropertyChanged(nameof(ReviewNotesCounter));
    }

    [RelayCommand]
    public async Task SaveChangesAsync()
    {
        switch (Type)
        {
            case ItemType.Project:
                await _taskService.UpdateProjectAsync(ItemId, Title, Description, ReviewNotes);
                await _taskService.UpdateProjectStatusAsync(ItemId, Status);
                break;
            case ItemType.HeadTask:
                await _taskService.UpdateHeadTaskAsync(ItemId, Title, Description, ReviewNotes);
                await _taskService.UpdateHeadTaskStatusAsync(ItemId, Status);
                break;
            case ItemType.SubTask:
                await _taskService.UpdateSubTaskAsync(ItemId, Title, Description, ReviewNotes);
                await _taskService.UpdateSubTaskStatusAsync(ItemId, Status);
                break;
        }
    }

    [RelayCommand]
    public async Task AddAttachmentAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;

        DotDone.Core.Models.Attachment? att = null;
        switch (Type)
        {
            case ItemType.Project:
                att = await _taskService.AddAttachmentToProjectAsync(ItemId, filePath);
                break;
            case ItemType.HeadTask:
                att = await _taskService.AddAttachmentToHeadTaskAsync(ItemId, filePath);
                break;
            case ItemType.SubTask:
                att = await _taskService.AddAttachmentToSubTaskAsync(ItemId, filePath);
                break;
        }

        if (att != null)
        {
            Attachments.Add(AttachmentViewModel.FromModel(att));
        }
    }

    [RelayCommand]
    public async Task DeleteAttachmentAsync(AttachmentViewModel attachment)
    {
        await _taskService.DeleteAttachmentAsync(attachment.Id);
        Attachments.Remove(attachment);
    }

    [RelayCommand]
    public void OpenAttachment(AttachmentViewModel attachment)
    {
        var fullPath = _fileStorage.GetFullPath(attachment.RelativePath);
        if (File.Exists(fullPath))
        {
            new Process
            {
                StartInfo = new ProcessStartInfo(fullPath) { UseShellExecute = true }
            }.Start();
        }
    }
}
