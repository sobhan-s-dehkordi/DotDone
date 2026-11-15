using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DotDone.Core.Enums;

namespace DotDone.Services;

public interface ILocalizationService
{
    AppLanguage CurrentLanguage { get; set; }
    bool IsPersian => CurrentLanguage == AppLanguage.Persian;
    string this[string key] => GetString(key);
    string ToLocalDigits(string input);
    string FormatDate(DateTime dateTime);
    string FormatDate(DateOnly dateOnly);
    string GetStatusText(ItemStatus status);
    string GetString(string key);
}

public class LocalizationService : ILocalizationService
{
    private readonly PersianCalendar _persianCalendar = new();
    public AppLanguage CurrentLanguage { get; set; } = AppLanguage.Persian;

    public string ToLocalDigits(string input)
    {
        if (string.IsNullOrEmpty(input) || CurrentLanguage != AppLanguage.Persian)
            return input;

        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            if (c >= '0' && c <= '9')
            {
                sb.Append((char)('۰' + (c - '0')));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    public string FormatDate(DateTime dateTime)
    {
        return FormatDate(DateOnly.FromDateTime(dateTime));
    }

    public string FormatDate(DateOnly dateOnly)
    {
        if (CurrentLanguage == AppLanguage.Persian)
        {
            var dt = dateOnly.ToDateTime(TimeOnly.MinValue);
            int year = _persianCalendar.GetYear(dt);
            int month = _persianCalendar.GetMonth(dt);
            int day = _persianCalendar.GetDayOfMonth(dt);
            var raw = $"{year:0000}/{month:00}/{day:00}";
            return ToLocalDigits(raw);
        }
        else
        {
            return dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }

    public string GetStatusText(ItemStatus status)
    {
        if (CurrentLanguage == AppLanguage.Persian)
        {
            return status switch
            {
                ItemStatus.Todo => "انجام نشده",
                ItemStatus.InProgress => "درحال انجام",
                ItemStatus.Done => "انجام شده",
                _ => status.ToString()
            };
        }
        else
        {
            return status switch
            {
                ItemStatus.Todo => "To Do",
                ItemStatus.InProgress => "In Progress",
                ItemStatus.Done => "Done",
                _ => status.ToString()
            };
        }
    }

    public string GetString(string key)
    {
        if (CurrentLanguage == AppLanguage.Persian)
        {
            return key switch
            {
                "AppTitle" => "دات‌دان",
                "AppSubtitle" => "DotDone",
                "SelectRepositoryPlaceholder" => "انتخاب مخزن...",
                "NewRepoTooltip" => "ایجاد مخزن جدید",
                "NewRepoDialogTitle" => "ایجاد مخزن جدید",
                "NewRepoPlaceholder" => "عنوان مخزن جدید...",
                "EnableStreakCheckbox" => "فعال‌سازی روزشمار استریک (شبیه دولینگو)",
                "StreakBadge" => "روز استریک",
                "GraphFilterAll" => "گراف: همه مخازن",
                "GraphFilterProject" => "گراف: این پروژه",
                "ActivityGraph" => "گراف فعالیت",
                "GraphScoreHint" => "(انجام شده = ۲ امتیاز | درحال انجام = ۱ امتیاز)",
                "DaysActive" => "روز فعال",
                "TasksDone" => "انجام شده",
                "Projects" => "پروژه‌ها",
                "NewProjectBtn" => "+ پروژه جدید",
                "NewProjectDialogTitle" => "ایجاد پروژه جدید",
                "NewProjectPlaceholder" => "عنوان پروژه جدید...",
                "NoProjectSelected" => "پروژه‌ای انتخاب نشده است",
                "ProjectDetailsBtn" => "جزئیات پروژه",
                "QuickAddTaskPlaceholder" => "+ اضافه کردن تسک جدید (اینتر را بزنید)...",
                "QuickAddSubTaskPlaceholder" => "+ اضافه کردن زیرتسک جدید...",
                "MenuDetails" => "جزئیات و ضمائم",
                "MenuTransfer" => "کپی یا جابجایی به پروژه دیگر...",
                "MenuDelete" => "حذف تسک",
                "TransferTaskTooltip" => "کپی یا جابجایی به پروژه دیگر...",
                "CreateTask" => "ایجاد",
                "EditTask" => "ویرایش",
                "ViewTask" => "نمایش",
                "DeleteTask" => "حذف",
                "NewTaskDialogTitle" => "ایجاد تسک جدید",
                "NewTaskPlaceholder" => "عنوان تسک جدید...",
                "TaskSummaryDialogTitle" => "اطلاعات تسک",
                "SubTaskSummaryDialogTitle" => "اطلاعات زیرتسک",
                "DeleteTaskConfirmTitle" => "حذف تسک",
                "DeleteTaskConfirmMessageFormat" => "آیا از حذف تسک «{0}» اطمینان دارید؟",
                "DeleteSubTaskConfirmTitle" => "حذف زیرتسک",
                "DeleteSubTaskConfirmMessageFormat" => "آیا از حذف زیرتسک «{0}» اطمینان دارید؟",
                "NoTaskSelectedPrompt" => "لطفاً ابتدا یک تسک را انتخاب کنید.",
                "SubtasksBadge" => "زیرتسک",
                "LangSwitchLabel" => "فارسی / EN",
                "ThemeSwitchTooltip" => "تغییر تم تاریک / روشن",

                // Dialogs & Fields
                "Title" => "عنوان",
                "TitlePlaceholder" => "عنوان تسک یا پروژه",
                "Status" => "وضعیت",
                "StatusTodo" => "انجام نشده",
                "StatusInProgress" => "درحال انجام",
                "StatusDone" => "انجام شده",
                "Description" => "توضیحات",
                "DescriptionPlaceholder" => "توضیحات مربوط به این آیتم (حداکثر ۵۰۰ کاراکتر)",
                "ReviewNotes" => "توضیحات تکمیلی (پس از انجام / بررسی)",
                "ReviewNotesPlaceholder" => "یادداشت‌ها و نتیجه پس از بررسی یا انجام (حداکثر ۵۰۰ کاراکتر)",
                "Attachments" => "فایل‌های پیوست",
                "AddFile" => "افزودن فایل...",
                "OpenFile" => "باز کردن",
                "Delete" => "حذف",
                "Save" => "ذخیره",
                "Cancel" => "انصراف",
                "Apply" => "انجام",
                "CreatedDateLabel" => "تاریخ ایجاد:",
                "ModifiedDateLabel" => "آخرین تغییر:",

                // Confirmations
                "DeleteProjectConfirmTitle" => "حذف پروژه",
                "DeleteProjectConfirmMessage" => "آیا از حذف این پروژه و تمام تسک‌های آن اطمینان دارید؟",
                "DeleteProjectConfirmMessageFormat" => "آیا از حذف پروژه «{0}» و تمام تسک‌های آن اطمینان دارید؟",
                "YesDelete" => "بله، حذف شود",
                "ChangeStatusConfirmTitle" => "تغییر وضعیت",
                "ChangeStatusConfirmMessageFormat" => "آیا از تغییر وضعیت «{0}» به «{1}» اطمینان دارید؟",
                "YesChange" => "بله، تغییر بده",
                "EditConfirmTitle" => "ثبت تغییرات",
                "EditConfirmMessageFormat" => "آیا از اعمال تغییرات برای «{0}» اطمینان دارید؟",
                "YesSave" => "بله، ذخیره شود",
                "DeleteAttachmentConfirmTitle" => "حذف پیوست",
                "DeleteAttachmentConfirmMessage" => "آیا از حذف این پیوست اطمینان دارید؟",
                "NoOtherProjectTitle" => "پروژه دیگری وجود ندارد",
                "NoOtherProjectMessage" => "برای کپی یا انتقال، ابتدا یک پروژه دیگر در مخزن ایجاد کنید.",
                "GotIt" => "متوجه شدم",
                "Create" => "ایجاد",
                "TaskPrefix" => "تسک:",
                "ProjectDetailsTitle" => "جزئیات پروژه",
                "TaskDetailsTitle" => "جزئیات تسک",
                "SubTaskDetailsTitle" => "جزئیات زیرتسک",

                // Transfer
                "TransferTitle" => "کپی یا انتقال تسک",
                "TransferTaskPrefix" => "تسک:",
                "TransferOperation" => "عملیات:",
                "TransferCopy" => "کپی کامل به همراه زیرتسک‌ها و ضمائم (با ریست وضعیت به انجام نشده)",
                "TransferMove" => "انتقال کامل به پروژه مقصد",
                "TargetProject" => "پروژه مقصد",

                // Streak recovery
                "StreakGapTitle" => "🔥 بررسی استریک و ثبت فعالیت آفلاین",
                "StreakGapMessage" => "شما در چند روز گذشته فعالیتی ثبت نکرده‌اید. آیا در این مدت فعالیت داشتید و مایلید استریک حفظ شود؟",
                "MissedDaysHeader" => "روزهای بدون فعالیت ثبت‌شده:",
                "SelectMissedTasksHeader" => "تسک‌هایی که در این روزها انجام دادید را انتخاب کنید:",
                "YesPreserveStreak" => "تایید و ثبت فعالیت",
                "NoResetStreak" => "صفر کردن استریک",

                // Heatmap Legend & Title
                "Less" => "کمتر",
                "More" => "بیشتر",
                "GraphTitleAllRepos" => "گراف فعالیت: همه مخازن",
                "GraphTitleProjectFormat" => "گراف فعالیت: {0}",

                // Global Search
                "GlobalSearchPlaceholder" => "جستجو...",
                "SearchResults" => "نتایج جستجو",
                "NoResultsFound" => "موردی یافت نشد",

                // Repository Management Bar
                "RepositoryLabel" => "مخزن:",
                "CreateRepo" => "ایجاد",
                "EditRepo" => "ویرایش",
                "ViewRepo" => "نمایش",
                "DeleteRepo" => "حذف",
                "FilterReposPlaceholder" => "جستجوی سریع در مخازن...",
                "RepoProjectsCount" => "پروژه",
                "EditRepoDialogTitle" => "ویرایش مخزن",
                "RepoDetailsDialogTitle" => "اطلاعات مخزن",
                "DeleteRepoConfirmTitle" => "حذف مخزن",
                "DeleteRepoConfirmMessageFormat" => "آیا از حذف مخزن «{0}» و کلیه پروژه‌ها و تسک‌های آن اطمینان دارید؟",
                "TotalProjects" => "تعداد پروژه‌ها:",
                "TotalTasks" => "تعداد کل تسک‌ها:",
                "StreakStatus" => "وضعیت استریک:",
                "Enabled" => "فعال",
                "Disabled" => "غیرفعال",

                // Project Management Bar
                "CreateProject" => "ایجاد",
                "EditProject" => "ویرایش",
                "ViewProject" => "نمایش",
                "DeleteProject" => "حذف",
                "ProjectSummaryDialogTitle" => "اطلاعات پروژه",
                "TasksSummary" => "خلاصه تسک‌ها:",
                "CompletedTasks" => "انجام‌شده:",
                "InProgressTasks" => "درحال انجام:",
                "TodoTasks" => "انجام‌نشده:",
                "SelectProjectPrompt" => "برای مشاهده و مدیریت تسک‌ها، ابتدا یک پروژه را از لیست انتخاب کنید.",

                // App Info & Metadata
                "VersionLabel" => "ورژن:",
                "ReleaseDateLabel" => "تاریخ انتشار:",
                "ProducerLabel" => "تولید کننده:",
                "AboutDotDone" => "درباره دات‌دان",
                "Close" => "بستن",

                _ => key
            };
        }
        else
        {
            return key switch
            {
                "AppTitle" => "DotDone",
                "AppSubtitle" => "دات‌دان",
                "SelectRepositoryPlaceholder" => "Select repository...",
                "NewRepoTooltip" => "Create new repository",
                "NewRepoDialogTitle" => "New Repository",
                "NewRepoPlaceholder" => "New repository title...",
                "EnableStreakCheckbox" => "Enable streak counter (Duolingo style)",
                "StreakBadge" => "day streak",
                "GraphFilterAll" => "Graph: All Repos",
                "GraphFilterProject" => "Graph: This Project",
                "ActivityGraph" => "Activity Graph",
                "GraphScoreHint" => "(Done = 2 pts | In Progress = 1 pt)",
                "DaysActive" => "active days",
                "TasksDone" => "completed",
                "Projects" => "Projects",
                "NewProjectBtn" => "+ New Project",
                "NewProjectDialogTitle" => "New Project",
                "NewProjectPlaceholder" => "New project title...",
                "NoProjectSelected" => "No project selected",
                "ProjectDetailsBtn" => "Project Details",
                "QuickAddTaskPlaceholder" => "+ Add new task (Press Enter)...",
                "QuickAddSubTaskPlaceholder" => "+ Add new subtask...",
                "MenuDetails" => "Details & Attachments",
                "MenuTransfer" => "Copy or Move to another project...",
                "MenuDelete" => "Delete Task",
                "TransferTaskTooltip" => "Copy or Move to another project...",
                "CreateTask" => "Create",
                "EditTask" => "Edit",
                "ViewTask" => "View",
                "DeleteTask" => "Delete",
                "NewTaskDialogTitle" => "New Task",
                "NewTaskPlaceholder" => "New task title...",
                "TaskSummaryDialogTitle" => "Task Details",
                "SubTaskSummaryDialogTitle" => "Subtask Details",
                "DeleteTaskConfirmTitle" => "Delete Task",
                "DeleteTaskConfirmMessageFormat" => "Are you sure you want to delete task '{0}'?",
                "DeleteSubTaskConfirmTitle" => "Delete Subtask",
                "DeleteSubTaskConfirmMessageFormat" => "Are you sure you want to delete subtask '{0}'?",
                "NoTaskSelectedPrompt" => "Please select a task first.",
                "SubtasksBadge" => "subtasks",
                "LangSwitchLabel" => "English / فا",
                "ThemeSwitchTooltip" => "Toggle Dark / Light Theme",

                // Dialogs & Fields
                "Title" => "Title",
                "TitlePlaceholder" => "Task or project title",
                "Status" => "Status",
                "StatusTodo" => "To Do",
                "StatusInProgress" => "In Progress",
                "StatusDone" => "Done",
                "Description" => "Description",
                "DescriptionPlaceholder" => "Item description (max 500 characters)",
                "ReviewNotes" => "Completion / Review Notes",
                "ReviewNotesPlaceholder" => "Notes and outcome after review or completion (max 500 characters)",
                "Attachments" => "Attachments",
                "AddFile" => "Add file...",
                "OpenFile" => "Open",
                "Delete" => "Delete",
                "Save" => "Save",
                "Cancel" => "Cancel",
                "Apply" => "Apply",
                "CreatedDateLabel" => "Created:",
                "ModifiedDateLabel" => "Last Modified:",

                // Confirmations
                "DeleteProjectConfirmTitle" => "Delete Project",
                "DeleteProjectConfirmMessage" => "Are you sure you want to delete this project and all its tasks?",
                "DeleteProjectConfirmMessageFormat" => "Are you sure you want to delete project '{0}' and all its tasks?",
                "YesDelete" => "Yes, Delete",
                "ChangeStatusConfirmTitle" => "Change Status",
                "ChangeStatusConfirmMessageFormat" => "Are you sure you want to change the status of '{0}' to '{1}'?",
                "YesChange" => "Yes, Change",
                "EditConfirmTitle" => "Save Changes",
                "EditConfirmMessageFormat" => "Are you sure you want to save changes to '{0}'?",
                "YesSave" => "Yes, Save",
                "DeleteAttachmentConfirmTitle" => "Delete Attachment",
                "DeleteAttachmentConfirmMessage" => "Are you sure you want to delete this attachment?",
                "NoOtherProjectTitle" => "No Other Projects",
                "NoOtherProjectMessage" => "To copy or move, please create another project in the repository first.",
                "GotIt" => "Got it",
                "Create" => "Create",
                "TaskPrefix" => "Task:",
                "ProjectDetailsTitle" => "Project Details",
                "TaskDetailsTitle" => "Task Details",
                "SubTaskDetailsTitle" => "Subtask Details",

                // Transfer
                "TransferTitle" => "Copy or Move Task",
                "TransferTaskPrefix" => "Task:",
                "TransferOperation" => "Operation:",
                "TransferCopy" => "Full copy with subtasks & attachments (resets status to To Do)",
                "TransferMove" => "Move to target project",
                "TargetProject" => "Target Project",

                // Streak recovery
                "StreakGapTitle" => "🔥 Streak Continuity & Offline Check-in",
                "StreakGapMessage" => "No activity was recorded over recent days. Did you maintain activity during this offline period?",
                "MissedDaysHeader" => "Days without recorded activity:",
                "SelectMissedTasksHeader" => "Select tasks you completed during these days:",
                "YesPreserveStreak" => "Confirm & Log Activity",
                "NoResetStreak" => "Reset Streak",

                // Heatmap Legend & Title
                "Less" => "Less",
                "More" => "More",
                "GraphTitleAllRepos" => "Activity Graph: All Repositories",
                "GraphTitleProjectFormat" => "Activity Graph: {0}",

                // Global Search
                "GlobalSearchPlaceholder" => "Search...",
                "SearchResults" => "Search Results",
                "NoResultsFound" => "No results found",

                // Repository Management Bar
                "RepositoryLabel" => "Repository:",
                "CreateRepo" => "Create",
                "EditRepo" => "Edit",
                "ViewRepo" => "View",
                "DeleteRepo" => "Delete",
                "FilterReposPlaceholder" => "Filter repositories...",
                "RepoProjectsCount" => "projects",
                "EditRepoDialogTitle" => "Edit Repository",
                "RepoDetailsDialogTitle" => "Repository Details",
                "DeleteRepoConfirmTitle" => "Delete Repository",
                "DeleteRepoConfirmMessageFormat" => "Are you sure you want to delete repository '{0}' and all its projects and tasks?",
                "TotalProjects" => "Total Projects:",
                "TotalTasks" => "Total Tasks:",
                "StreakStatus" => "Streak Status:",
                "Enabled" => "Enabled",
                "Disabled" => "Disabled",

                // Project Management Bar
                "CreateProject" => "Create",
                "EditProject" => "Edit",
                "ViewProject" => "View",
                "DeleteProject" => "Delete",
                "ProjectSummaryDialogTitle" => "Project Details",
                "TasksSummary" => "Tasks Summary:",
                "CompletedTasks" => "Done:",
                "InProgressTasks" => "In Progress:",
                "TodoTasks" => "To Do:",
                "SelectProjectPrompt" => "Please select a project from the list to view and manage tasks.",

                // App Info & Metadata
                "VersionLabel" => "Version:",
                "ReleaseDateLabel" => "Release Date:",
                "ProducerLabel" => "Producer:",
                "AboutDotDone" => "About DotDone",
                "Close" => "Close",

                _ => key
            };
        }
    }
}
