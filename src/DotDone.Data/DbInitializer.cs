using System;
using System.Linq;
using DotDone.Core.Enums;
using DotDone.Core.Models;
using DotDone.Data.Context;

namespace DotDone.Data;

public static class DbInitializer
{
    public static void Initialize(DotDoneDbContext db)
    {
        if (db.Repositories.Any()) return;

        var repo = new Repository
        {
            Title = "کارهای شخصی و روزمره",
            Description = "مخزن اصلی برای پیگیری اهداف و تسک‌های روزانه",
            IsStreakEnabled = true,
            CurrentStreak = 3,
            LastActiveDate = DateOnly.FromDateTime(DateTime.Now),
            CreatedDate = DateTime.UtcNow.AddDays(-10)
        };

        var project = new Project
        {
            Repository = repo,
            Title = "توسعه و راه‌اندازی دات‌دان (DotDone)",
            Description = "نرم‌افزار شخصی مدیریت تسک‌ها با ساختار درختی و گیمیفیکیشن",
            Status = ItemStatus.InProgress,
            CreatedDate = DateTime.UtcNow.AddDays(-5)
        };

        var task1 = new HeadTask
        {
            Project = project,
            Title = "پیاده‌سازی هسته معماری و مدل داده دات نت ۱۰",
            Description = "ایجاد ساختار چهار سطحی، مخازن EF Core و اتصال به SQLite",
            Status = ItemStatus.Done,
            ReviewNotes = "با موفقیت انجام شد و ۱۲ تست واحد خودکار برای آن پاس شد.",
            CreatedDate = DateTime.UtcNow.AddDays(-2),
            ModifiedDate = DateTime.UtcNow.AddDays(-1),
            SubTasks =
            {
                new SubTask { Title = "تعریف انتیتی‌ها و روابط پایگاه داده", Status = ItemStatus.Done },
                new SubTask { Title = "پیاده‌سازی لاجیک استریک دولینگو و ریکاوری غیبت", Status = ItemStatus.Done },
                new SubTask { Title = "پیاده‌سازی کپی عمیق و جابجایی تسک‌ها", Status = ItemStatus.Done }
            }
        };

        var task2 = new HeadTask
        {
            Project = project,
            Title = "طراحی رابط کاربری مدرن با WinUI 3 و فونت وزیرمتن",
            Description = "ایجاد صفحه اصلی مینیمال، گراف فعالیت گیت و دیالوگ‌های دبل‌کلیک",
            Status = ItemStatus.InProgress,
            CreatedDate = DateTime.UtcNow.AddDays(-1),
            SubTasks =
            {
                new SubTask { Title = "کنترل سفارشی مربع‌های فعالیت شبیه Git", Status = ItemStatus.Done },
                new SubTask { Title = "پشتیبانی دوزبانه فارسی و انگلیسی (RTL/LTR)", Status = ItemStatus.InProgress },
                new SubTask { Title = "دیالوگ مشاهده و ویرایش جزئیات با محدودیت ۵۰۰ کاراکتر", Status = ItemStatus.InProgress }
            }
        };

        var task3 = new HeadTask
        {
            Project = project,
            Title = "بررسی روزشمار استریک و حفظ پیوستگی فعالیت‌ها",
            Description = "ثبت وضعیت درحال انجام یا انجام شده برای زنده نگه‌داشتن استریک",
            Status = ItemStatus.Todo,
            CreatedDate = DateTime.UtcNow
        };

        db.Repositories.Add(repo);
        db.Projects.Add(project);
        db.HeadTasks.AddRange(task1, task2, task3);

        // Add past activity logs for heatmap display
        var today = DateOnly.FromDateTime(DateTime.Now);
        db.ActivityLogs.AddRange(
            new DailyActivityLog { ActivityDate = today.AddDays(-2), Repository = repo, ProjectId = project.Id, StatusChangedTo = ItemStatus.Done, ActivityWeight = 2 },
            new DailyActivityLog { ActivityDate = today.AddDays(-1), Repository = repo, ProjectId = project.Id, StatusChangedTo = ItemStatus.Done, ActivityWeight = 2 },
            new DailyActivityLog { ActivityDate = today.AddDays(-1), Repository = repo, ProjectId = project.Id, StatusChangedTo = ItemStatus.InProgress, ActivityWeight = 1 },
            new DailyActivityLog { ActivityDate = today, Repository = repo, ProjectId = project.Id, StatusChangedTo = ItemStatus.InProgress, ActivityWeight = 1 },
            new DailyActivityLog { ActivityDate = today, Repository = repo, ProjectId = project.Id, StatusChangedTo = ItemStatus.Done, ActivityWeight = 2 }
        );

        db.SaveChanges();
    }
}
