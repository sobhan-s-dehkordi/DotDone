using System;
using DotDone.Core.Enums;
using DotDone.Services;
using FluentAssertions;
using Xunit;

namespace DotDone.Tests;

public class LocalizationTests
{
    private readonly ILocalizationService _localization = new LocalizationService();

    [Fact]
    public void ToLocalDigits_ShouldConvertLatinNumbersToPersian_WhenInPersianMode()
    {
        _localization.CurrentLanguage = AppLanguage.Persian;
        var result = _localization.ToLocalDigits("1234567890");
        result.Should().Be("۱۲۳۴۵۶۷۸۹۰");
    }

    [Fact]
    public void ToLocalDigits_ShouldPreserveLatinNumbers_WhenInEnglishMode()
    {
        _localization.CurrentLanguage = AppLanguage.English;
        var result = _localization.ToLocalDigits("1234567890");
        result.Should().Be("1234567890");
    }

    [Fact]
    public void FormatDate_ShouldReturnPersianDateWithPersianDigits_InPersianMode()
    {
        _localization.CurrentLanguage = AppLanguage.Persian;
        var dt = new DateTime(2026, 3, 21); // Farvardin 1, 1405
        var formatted = _localization.FormatDate(dt);

        formatted.Should().Be("۱۴۰۵/۰۱/۰۱");
    }

    [Fact]
    public void FormatDate_ShouldReturnStandardIsoDate_InEnglishMode()
    {
        _localization.CurrentLanguage = AppLanguage.English;
        var dt = new DateTime(2026, 3, 21);
        var formatted = _localization.FormatDate(dt);

        formatted.Should().Be("2026-03-21");
    }

    [Theory]
    [InlineData(ItemStatus.Todo, "انجام نشده", "To Do")]
    [InlineData(ItemStatus.InProgress, "درحال انجام", "In Progress")]
    [InlineData(ItemStatus.Done, "انجام شده", "Done")]
    public void GetStatusText_ShouldReturnCorrectTranslation(ItemStatus status, string expectedFa, string expectedEn)
    {
        _localization.CurrentLanguage = AppLanguage.Persian;
        _localization.GetStatusText(status).Should().Be(expectedFa);

        _localization.CurrentLanguage = AppLanguage.English;
        _localization.GetStatusText(status).Should().Be(expectedEn);
    }

    [Fact]
    public void DateAndLabelAlignment_ShouldProduceValidFormattedStrings_InBothLanguages()
    {
        // Check Persian order
        _localization.CurrentLanguage = AppLanguage.Persian;
        var labelFa = _localization.GetString("CreatedDateLabel");
        var dateFa = _localization.FormatDate(new DateTime(2026, 3, 21));
        var fullFa = $"{labelFa} {dateFa}";
        fullFa.Should().Be("تاریخ ایجاد: ۱۴۰۵/۰۱/۰۱");

        // Check English order
        _localization.CurrentLanguage = AppLanguage.English;
        var labelEn = _localization.GetString("CreatedDateLabel");
        var dateEn = _localization.FormatDate(new DateTime(2026, 3, 21));
        var fullEn = $"{labelEn} {dateEn}";
        fullEn.Should().Be("Created: 2026-03-21");
    }

    [Fact]
    public void AllEssentialLocalizationKeys_ShouldExistInBothLanguages()
    {
        string[] keys =
        {
            "AppTitle", "AppSubtitle", "SelectRepositoryPlaceholder", "NewRepoTooltip",
            "NewRepoDialogTitle", "NewRepoPlaceholder", "EnableStreakCheckbox", "StreakBadge",
            "GraphFilterAll", "GraphFilterProject", "ActivityGraph", "GraphScoreHint",
            "DaysActive", "TasksDone", "Projects", "NewProjectBtn", "NewProjectDialogTitle",
            "NewProjectPlaceholder", "NoProjectSelected", "ProjectDetailsBtn",
            "QuickAddTaskPlaceholder", "QuickAddSubTaskPlaceholder", "MenuDetails",
            "MenuTransfer", "MenuDelete", "LangSwitchLabel", "ThemeSwitchTooltip",
            "Title", "TitlePlaceholder", "Status", "StatusTodo", "StatusInProgress",
            "StatusDone", "Description", "DescriptionPlaceholder", "ReviewNotes",
            "ReviewNotesPlaceholder", "Attachments", "AddFile", "OpenFile", "Delete",
            "Save", "Cancel", "Apply", "CreatedDateLabel", "ModifiedDateLabel",
            "DeleteProjectConfirmTitle", "DeleteProjectConfirmMessageFormat", "YesDelete",
            "NoOtherProjectTitle", "NoOtherProjectMessage", "GotIt", "Create",
            "TransferTitle", "TransferTaskPrefix", "TransferOperation", "TransferCopy",
            "TransferMove", "TargetProject", "StreakGapTitle", "StreakGapMessage",
            "MissedDaysHeader", "SelectMissedTasksHeader", "YesPreserveStreak",
            "NoResetStreak", "Less", "More",
            "GlobalSearchPlaceholder", "SearchResults", "NoResultsFound", "RepositoryLabel",
            "CreateRepo", "EditRepo", "ViewRepo", "DeleteRepo", "FilterReposPlaceholder",
            "RepoProjectsCount", "EditRepoDialogTitle", "RepoDetailsDialogTitle",
            "DeleteRepoConfirmTitle", "DeleteRepoConfirmMessageFormat", "TotalProjects",
            "TotalTasks", "StreakStatus", "Enabled", "Disabled", "GraphTitleAllRepos",
            "GraphTitleProjectFormat", "CreateProject", "EditProject", "ViewProject",
            "DeleteProject", "ProjectSummaryDialogTitle", "TasksSummary", "CompletedTasks",
            "InProgressTasks", "TodoTasks", "SelectProjectPrompt",
            "TransferTaskTooltip", "CreateTask", "EditTask", "ViewTask", "DeleteTask",
            "NewTaskDialogTitle", "NewTaskPlaceholder", "TaskSummaryDialogTitle",
            "SubTaskSummaryDialogTitle", "DeleteTaskConfirmTitle", "DeleteTaskConfirmMessageFormat",
            "DeleteSubTaskConfirmTitle", "DeleteSubTaskConfirmMessageFormat", "NoTaskSelectedPrompt",
            "ChangeStatusConfirmTitle", "ChangeStatusConfirmMessageFormat", "YesChange",
            "EditConfirmTitle", "EditConfirmMessageFormat", "YesSave",
            "DeleteAttachmentConfirmTitle", "DeleteAttachmentConfirmMessage",
            "VersionLabel", "ReleaseDateLabel", "ProducerLabel", "AboutDotDone", "Close"
        };

        foreach (var lang in new[] { AppLanguage.Persian, AppLanguage.English })
        {
            _localization.CurrentLanguage = lang;
            foreach (var key in keys)
            {
                var val = _localization.GetString(key);
                val.Should().NotBeNullOrWhiteSpace($"Key '{key}' should have a translation in {lang}");
                if (lang == AppLanguage.Persian)
                {
                    val.Should().NotBe(key, $"Key '{key}' in Persian should not fallback to its English key name");
                }
            }
        }
    }
}
