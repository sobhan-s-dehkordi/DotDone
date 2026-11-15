using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotDone.Core.Enums;
using DotDone.Services;

namespace DotDone.UI.ViewModels;

public partial class RetroactiveItemOption : ObservableObject
{
    public int Id { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private DateOnly _selectedDate;

    [ObservableProperty]
    private ItemStatus _status = ItemStatus.Done;
}

public partial class StreakGapDialogViewModel : ObservableObject
{
    private readonly IStreakService _streakService;
    private readonly ILocalizationService _localization;

    public StreakGapResult GapResult { get; }

    [ObservableProperty]
    private string _questionMessage = string.Empty;

    [ObservableProperty]
    private bool _isRecovering = false;

    public ObservableCollection<DateOnly> MissedDays { get; } = new();
    public ObservableCollection<string> FormattedMissedDays { get; } = new();
    public ObservableCollection<RetroactiveItemOption> AvailableItems { get; } = new();

    public Microsoft.UI.Xaml.FlowDirection FlowDirection => _localization.IsPersian
        ? Microsoft.UI.Xaml.FlowDirection.RightToLeft
        : Microsoft.UI.Xaml.FlowDirection.LeftToRight;

    public string L_Title => _localization.GetString("StreakGapTitle");
    public string L_YesPreserveStreak => _localization.GetString("YesPreserveStreak");
    public string L_NoResetStreak => _localization.GetString("NoResetStreak");
    public string L_MissedDaysHeader => _localization.GetString("MissedDaysHeader");
    public string L_SelectMissedTasksHeader => _localization.GetString("SelectMissedTasksHeader");

    public StreakGapDialogViewModel(
        StreakGapResult gapResult,
        IEnumerable<(int id, string type, string title)> eligibleItems,
        IStreakService streakService,
        ILocalizationService localization)
    {
        _streakService = streakService;
        _localization = localization;
        GapResult = gapResult;

        QuestionMessage = _localization.GetString("StreakGapMessage");

        foreach (var day in gapResult.MissedDays)
        {
            MissedDays.Add(day);
            FormattedMissedDays.Add(_localization.FormatDate(day));
        }

        var defaultDate = gapResult.MissedDays.FirstOrDefault();
        foreach (var item in eligibleItems)
        {
            AvailableItems.Add(new RetroactiveItemOption
            {
                Id = item.id,
                ItemType = item.type,
                Title = item.title,
                SelectedDate = defaultDate,
                Status = ItemStatus.Done
            });
        }
    }

    [RelayCommand]
    public void ChooseYesActive()
    {
        IsRecovering = true;
    }

    [RelayCommand]
    public async Task ConfirmRecoveryAsync()
    {
        var selected = AvailableItems
            .Where(x => x.IsSelected)
            .Select(x => (x.Id, x.ItemType, x.SelectedDate, x.Status))
            .ToList();

        if (selected.Count > 0)
        {
            await _streakService.RecoverStreakAsync(GapResult.RepositoryId, selected);
        }
    }

    [RelayCommand]
    public async Task ChooseNoResetAsync()
    {
        await _streakService.ResetStreakAsync(GapResult.RepositoryId);
    }
}
