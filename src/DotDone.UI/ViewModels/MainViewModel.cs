using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotDone.Core.Enums;
using DotDone.Services;

namespace DotDone.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITaskService _taskService;
    private readonly ITaskTransferService _transferService;
    private readonly IStreakService _streakService;
    private readonly IActivityHeatmapService _heatmapService;
    private readonly ILocalizationService _localization;

    public ILocalizationService Localization => _localization;

    [ObservableProperty]
    private RepositoryViewModel? _selectedRepository;

    [ObservableProperty]
    private ProjectViewModel? _selectedProject;

    [ObservableProperty]
    private HeadTaskViewModel? _selectedHeadTask;

    [ObservableProperty]
    private HeatmapSummaryDto? _currentHeatmap;

    [ObservableProperty]
    private bool _isHeatmapFilteredByProject = false;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private AppLanguage _currentLanguage = AppLanguage.Persian;

    [ObservableProperty]
    private AppTheme _currentTheme = AppTheme.Dark;

    public ObservableCollection<RepositoryViewModel> Repositories { get; } = new();
    public ObservableCollection<RepositoryViewModel> FilteredRepositories { get; } = new();
    public ObservableCollection<SearchResultItemDto> SearchResults { get; } = new();

    [ObservableProperty]
    private string _repoFilterText = string.Empty;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    partial void OnRepoFilterTextChanged(string value)
    {
        UpdateFilteredRepositories();
    }

    public void UpdateFilteredRepositories()
    {
        FilteredRepositories.Clear();
        var q = (RepoFilterText ?? string.Empty).Trim().ToLower();
        foreach (var r in Repositories)
        {
            if (string.IsNullOrEmpty(q) || r.Title.ToLower().Contains(q) || (r.Description != null && r.Description.ToLower().Contains(q)))
            {
                FilteredRepositories.Add(r);
            }
        }
    }

    #region Localized UI Properties
    public string L_AppTitle => _localization.GetString("AppTitle");
    public string L_AppSubtitle => _localization.GetString("AppSubtitle");
    public string L_SelectRepositoryPlaceholder => _localization.GetString("SelectRepositoryPlaceholder");
    public string L_NewRepoTooltip => _localization.GetString("NewRepoTooltip");
    public string L_StreakBadge => _localization.GetString("StreakBadge");
    public string L_ActivityGraph => _localization.GetString("ActivityGraph");
    public string L_GraphScoreHint => _localization.GetString("GraphScoreHint");
    public string L_GraphFilterLabel => IsHeatmapFilteredByProject
        ? _localization.GetString("GraphFilterProject")
        : _localization.GetString("GraphFilterAll");
    public string L_ProjectsTitle => _localization.GetString("Projects");
    public string L_NewProjectBtn => _localization.GetString("NewProjectBtn");
    public string L_CreateProject => _localization.GetString("CreateProject");
    public string L_EditProject => _localization.GetString("EditProject");
    public string L_ViewProject => _localization.GetString("ViewProject");
    public string L_DeleteProject => _localization.GetString("DeleteProject");
    public string L_CreateTask => _localization.GetString("CreateTask");
    public string L_EditTask => _localization.GetString("EditTask");
    public string L_ViewTask => _localization.GetString("ViewTask");
    public string L_DeleteTask => _localization.GetString("DeleteTask");
    public string L_TransferTaskTooltip => _localization.GetString("TransferTaskTooltip");
    public string L_NoTaskSelectedPrompt => _localization.GetString("NoTaskSelectedPrompt");
    public string L_NoProjectSelected => _localization.GetString("NoProjectSelected");
    public string L_SelectProjectPrompt => _localization.GetString("SelectProjectPrompt");
    public bool IsProjectSelected => SelectedProject != null;
    public string L_ProjectDetailsBtn => _localization.GetString("ProjectDetailsBtn");
    public string L_QuickAddTaskPlaceholder => _localization.GetString("QuickAddTaskPlaceholder");
    public string L_QuickAddSubTaskPlaceholder => _localization.GetString("QuickAddSubTaskPlaceholder");
    public string L_MenuDetails => _localization.GetString("MenuDetails");
    public string L_MenuTransfer => _localization.GetString("MenuTransfer");
    public string L_MenuDelete => _localization.GetString("MenuDelete");
    public string L_LangSwitchLabel => _localization.GetString("LangSwitchLabel");
    public string L_ThemeSwitchTooltip => _localization.GetString("ThemeSwitchTooltip");
    public string L_GlobalSearchPlaceholder => _localization.GetString("GlobalSearchPlaceholder");
    public string L_RepositoryLabel => _localization.GetString("RepositoryLabel");
    public string L_CreateRepo => _localization.GetString("CreateRepo");
    public string L_EditRepo => _localization.GetString("EditRepo");
    public string L_ViewRepo => _localization.GetString("ViewRepo");
    public string L_DeleteRepo => _localization.GetString("DeleteRepo");
    public string L_FilterReposPlaceholder => _localization.GetString("FilterReposPlaceholder");

    // App Information & Metadata (Values strictly Gregorian/English)
    public string AppVersion => "1.0.0";
    public string AppReleaseDate => "2026-11-08";
    public string AppProducer => "Sobhan Soleyman";

    public string L_VersionLabel => _localization.GetString("VersionLabel");
    public string L_ReleaseDateLabel => _localization.GetString("ReleaseDateLabel");
    public string L_ProducerLabel => _localization.GetString("ProducerLabel");
    public string L_AboutDotDone => _localization.GetString("AboutDotDone");
    public string L_Close => _localization.GetString("Close");

    public string ActivityGraphTitle
    {
        get
        {
            var repoTitle = SelectedRepository?.Title;
            if (string.IsNullOrWhiteSpace(repoTitle))
            {
                return _localization.GetString("ActivityGraph");
            }

            if (_localization.IsPersian)
            {
                return $"گراف فعالیت {repoTitle}";
            }

            return $"Activity Graph {repoTitle}";
        }
    }

    public string SelectedProjectTitle => SelectedProject?.Title ?? L_NoProjectSelected;

    public string HeatmapActiveDaysText
    {
        get
        {
            var count = CurrentHeatmap?.TotalActiveDays ?? 0;
            var num = _localization.ToLocalDigits(count.ToString());
            return $"{num} {_localization.GetString("DaysActive")}";
        }
    }

    public string HeatmapDoneText
    {
        get
        {
            var count = CurrentHeatmap?.TotalDoneCount ?? 0;
            var num = _localization.ToLocalDigits(count.ToString());
            return $"{num} {_localization.GetString("TasksDone")}";
        }
    }

    public bool IsStreakVisible => SelectedRepository?.IsStreakEnabled == true;

    public string FormattedStreakCount
    {
        get
        {
            var count = SelectedRepository?.CurrentStreak ?? 0;
            return _localization.ToLocalDigits(count.ToString());
        }
    }
    #endregion

    public MainViewModel(
        ITaskService taskService,
        ITaskTransferService transferService,
        IStreakService streakService,
        IActivityHeatmapService heatmapService,
        ILocalizationService localization)
    {
        _taskService = taskService;
        _transferService = transferService;
        _streakService = streakService;
        _heatmapService = heatmapService;
        _localization = localization;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            await LoadRepositoriesAsync();
            await RefreshHeatmapAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task LoadRepositoriesAsync()
    {
        Repositories.Clear();
        var repos = await _taskService.GetAllRepositoriesAsync();
        foreach (var r in repos)
        {
            Repositories.Add(RepositoryViewModel.FromModel(r));
        }
        UpdateFilteredRepositories();

        if (SelectedRepository == null && Repositories.Count > 0)
        {
            SelectedRepository = Repositories[0];
        }
    }

    partial void OnCurrentHeatmapChanged(HeatmapSummaryDto? value)
    {
        OnPropertyChanged(nameof(HeatmapActiveDaysText));
        OnPropertyChanged(nameof(HeatmapDoneText));
    }

    partial void OnSelectedRepositoryChanged(RepositoryViewModel? value)
    {
        OnPropertyChanged(nameof(IsStreakVisible));
        OnPropertyChanged(nameof(FormattedStreakCount));
        OnPropertyChanged(nameof(ActivityGraphTitle));
        SelectedProject = null;
        _ = RefreshHeatmapAsync();
    }

    partial void OnSelectedProjectChanged(ProjectViewModel? value)
    {
        SelectedHeadTask = null;
        OnPropertyChanged(nameof(SelectedProjectTitle));
        OnPropertyChanged(nameof(IsProjectSelected));
    }

    [RelayCommand]
    public async Task RefreshHeatmapAsync()
    {
        int? repoId = SelectedRepository?.Id;
        CurrentHeatmap = await _heatmapService.GetActivityHeatmapAsync(repoId, null);
    }

    [RelayCommand]
    public async Task ToggleHeatmapFilterAsync()
    {
        IsHeatmapFilteredByProject = !IsHeatmapFilteredByProject;
        OnPropertyChanged(nameof(L_GraphFilterLabel));
        OnPropertyChanged(nameof(ActivityGraphTitle));
        await RefreshHeatmapAsync();
    }

    #region Create Operations
    [RelayCommand]
    public async Task CreateRepositoryAsync((string title, string? description, bool isStreakEnabled) param)
    {
        if (string.IsNullOrWhiteSpace(param.title)) return;
        var created = await _taskService.CreateRepositoryAsync(param.title, param.description, param.isStreakEnabled);
        var vm = RepositoryViewModel.FromModel(created);
        Repositories.Add(vm);
        UpdateFilteredRepositories();
        SelectedRepository = vm;
    }

    [RelayCommand]
    public async Task UpdateRepositoryAsync((int id, string title, string? description, bool isStreakEnabled) param)
    {
        var updated = await _taskService.UpdateRepositoryAsync(param.id, param.title, param.description, param.isStreakEnabled);
        var vm = Repositories.FirstOrDefault(r => r.Id == param.id);
        if (vm != null)
        {
            vm.Title = updated.Title;
            vm.Description = updated.Description;
            vm.IsStreakEnabled = updated.IsStreakEnabled;
            OnPropertyChanged(nameof(IsStreakVisible));
            OnPropertyChanged(nameof(FormattedStreakCount));
            UpdateFilteredRepositories();
        }
    }

    [RelayCommand]
    public async Task DeleteRepositoryAsync(RepositoryViewModel repo)
    {
        await _taskService.DeleteRepositoryAsync(repo.Id);
        Repositories.Remove(repo);
        UpdateFilteredRepositories();
        if (SelectedRepository == repo)
        {
            SelectedRepository = Repositories.FirstOrDefault();
        }
        await RefreshHeatmapAsync();
    }

    [RelayCommand]
    public async Task PerformSearchAsync(string query)
    {
        SearchQuery = query;
        SearchResults.Clear();
        if (string.IsNullOrWhiteSpace(query)) return;

        var items = await _taskService.GlobalSearchAsync(query);
        foreach (var it in items)
        {
            SearchResults.Add(it);
        }
    }

    [RelayCommand]
    public void SelectSearchResult(SearchResultItemDto item)
    {
        if (item == null) return;

        var repo = Repositories.FirstOrDefault(r => r.Id == item.RepositoryId);
        if (repo != null)
        {
            SelectedRepository = repo;

            if (item.ProjectId.HasValue)
            {
                var proj = repo.Projects.FirstOrDefault(p => p.Id == item.ProjectId.Value);
                if (proj != null)
                {
                    SelectedProject = proj;

                    if (item.HeadTaskId.HasValue)
                    {
                        var task = proj.HeadTasks.FirstOrDefault(t => t.Id == item.HeadTaskId.Value);
                        if (task != null)
                        {
                            task.IsExpanded = true;
                            SelectedHeadTask = task;
                        }
                    }
                }
            }
        }
    }

    [RelayCommand]
    public async Task CreateProjectAsync(string title)
    {
        if (SelectedRepository == null || string.IsNullOrWhiteSpace(title)) return;
        var created = await _taskService.CreateProjectAsync(SelectedRepository.Id, title);
        var vm = ProjectViewModel.FromModel(created);
        SelectedRepository.Projects.Add(vm);
        SelectedProject = vm;
    }

    [RelayCommand]
    public async Task CreateHeadTaskAsync(string title)
    {
        if (SelectedProject == null || string.IsNullOrWhiteSpace(title)) return;
        var created = await _taskService.CreateHeadTaskAsync(SelectedProject.Id, title);
        var vm = HeadTaskViewModel.FromModel(created);
        SelectedProject.HeadTasks.Insert(0, vm);
        SelectedHeadTask = vm;
        await RefreshHeatmapAsync();
    }

    [RelayCommand]
    public async Task CreateHeadTaskWithDetailsAsync((string title, string? description) param)
    {
        if (SelectedProject == null || string.IsNullOrWhiteSpace(param.title)) return;
        var created = await _taskService.CreateHeadTaskAsync(SelectedProject.Id, param.title, param.description);
        var vm = HeadTaskViewModel.FromModel(created);
        SelectedProject.HeadTasks.Insert(0, vm);
        SelectedHeadTask = vm;
        await RefreshHeatmapAsync();
    }

    [RelayCommand]
    public async Task CreateSubTaskAsync((HeadTaskViewModel parent, string title) param)
    {
        if (param.parent == null || string.IsNullOrWhiteSpace(param.title)) return;
        var created = await _taskService.CreateSubTaskAsync(param.parent.Id, param.title);
        var vm = SubTaskViewModel.FromModel(created);
        param.parent.SubTasks.Insert(0, vm);
        param.parent.NotifyProgressChanged();
    }
    #endregion

    #region Status Operations
    [RelayCommand]
    public async Task UpdateHeadTaskStatusAsync((HeadTaskViewModel task, ItemStatus status) param)
    {
        await _taskService.UpdateHeadTaskStatusAsync(param.task.Id, param.status);
        param.task.Status = param.status;
        param.task.ModifiedDate = DateTime.UtcNow;

        if (SelectedRepository != null)
        {
            var updated = await _taskService.GetRepositoryByIdAsync(SelectedRepository.Id);
            if (updated != null)
            {
                SelectedRepository.CurrentStreak = updated.CurrentStreak;
                SelectedRepository.LastActiveDate = updated.LastActiveDate;
                OnPropertyChanged(nameof(FormattedStreakCount));
            }
        }
        await RefreshHeatmapAsync();
    }

    [RelayCommand]
    public async Task UpdateSubTaskStatusAsync((SubTaskViewModel sub, HeadTaskViewModel parent, ItemStatus status) param)
    {
        await _taskService.UpdateSubTaskStatusAsync(param.sub.Id, param.status);
        param.sub.Status = param.status;
        param.sub.ModifiedDate = DateTime.UtcNow;
        param.parent.NotifyProgressChanged();

        if (SelectedRepository != null)
        {
            var updated = await _taskService.GetRepositoryByIdAsync(SelectedRepository.Id);
            if (updated != null)
            {
                SelectedRepository.CurrentStreak = updated.CurrentStreak;
                SelectedRepository.LastActiveDate = updated.LastActiveDate;
                OnPropertyChanged(nameof(FormattedStreakCount));
            }
        }
        await RefreshHeatmapAsync();
    }
    #endregion

    #region Copy & Move Operations
    [RelayCommand]
    public async Task CopyHeadTaskAsync((HeadTaskViewModel task, int targetProjectId) param)
    {
        var copied = await _transferService.CopyHeadTaskAsync(param.task.Id, param.targetProjectId, resetStatusToTodo: true);
        var targetProj = Repositories.SelectMany(r => r.Projects).FirstOrDefault(p => p.Id == param.targetProjectId);
        targetProj?.HeadTasks.Insert(0, HeadTaskViewModel.FromModel(copied));
    }

    [RelayCommand]
    public async Task MoveHeadTaskAsync((HeadTaskViewModel task, int targetProjectId) param)
    {
        await _transferService.MoveHeadTaskAsync(param.task.Id, param.targetProjectId);
        if (SelectedProject != null)
        {
            SelectedProject.HeadTasks.Remove(param.task);
        }
        var targetProj = Repositories.SelectMany(r => r.Projects).FirstOrDefault(p => p.Id == param.targetProjectId);
        targetProj?.HeadTasks.Insert(0, param.task);
    }
    #endregion

    #region Delete Operations
    [RelayCommand]
    public async Task DeleteHeadTaskAsync(HeadTaskViewModel task)
    {
        await _taskService.DeleteHeadTaskAsync(task.Id);
        SelectedProject?.HeadTasks.Remove(task);
        if (SelectedHeadTask == task)
        {
            SelectedHeadTask = null;
        }

        if (SelectedRepository != null)
        {
            var updated = await _taskService.GetRepositoryByIdAsync(SelectedRepository.Id);
            if (updated != null)
            {
                SelectedRepository.CurrentStreak = updated.CurrentStreak;
                SelectedRepository.LastActiveDate = updated.LastActiveDate;
                OnPropertyChanged(nameof(FormattedStreakCount));
            }
        }
        await RefreshHeatmapAsync();
    }

    [RelayCommand]
    public async Task DeleteSubTaskAsync((SubTaskViewModel sub, HeadTaskViewModel parent) param)
    {
        await _taskService.DeleteSubTaskAsync(param.sub.Id);
        param.parent.SubTasks.Remove(param.sub);
        param.parent.NotifyProgressChanged();

        if (SelectedRepository != null)
        {
            var updated = await _taskService.GetRepositoryByIdAsync(SelectedRepository.Id);
            if (updated != null)
            {
                SelectedRepository.CurrentStreak = updated.CurrentStreak;
                SelectedRepository.LastActiveDate = updated.LastActiveDate;
                OnPropertyChanged(nameof(FormattedStreakCount));
            }
        }
        await RefreshHeatmapAsync();
    }

    [RelayCommand]
    public async Task DeleteProjectAsync(ProjectViewModel project)
    {
        await _taskService.DeleteProjectAsync(project.Id);
        SelectedRepository?.Projects.Remove(project);
        if (SelectedProject == project)
        {
            SelectedProject = SelectedRepository?.Projects.FirstOrDefault();
        }

        if (SelectedRepository != null)
        {
            var updated = await _taskService.GetRepositoryByIdAsync(SelectedRepository.Id);
            if (updated != null)
            {
                SelectedRepository.CurrentStreak = updated.CurrentStreak;
                SelectedRepository.LastActiveDate = updated.LastActiveDate;
                OnPropertyChanged(nameof(FormattedStreakCount));
            }
        }
        await RefreshHeatmapAsync();
    }
    #endregion

    #region Settings
    [RelayCommand]
    public void ToggleLanguage()
    {
        CurrentLanguage = CurrentLanguage == AppLanguage.Persian ? AppLanguage.English : AppLanguage.Persian;
        _localization.CurrentLanguage = CurrentLanguage;

        foreach (var repo in Repositories)
        {
            foreach (var proj in repo.Projects)
            {
                proj.NotifyLanguageChanged();
            }
        }

        // Force refresh all bindings on view model
        OnPropertyChanged(string.Empty);
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        CurrentTheme = CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
    }
    #endregion

    public async Task<StreakGapResult?> CheckActiveRepoStreakGapAsync()
    {
        if (SelectedRepository == null || !SelectedRepository.IsStreakEnabled)
            return null;

        return await _streakService.CheckRepositoryGapAsync(SelectedRepository.Id);
    }
}
