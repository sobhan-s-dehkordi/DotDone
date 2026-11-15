using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using DotDone.Data;
using DotDone.Data.Context;
using DotDone.Data.Services;
using DotDone.Services;
using DotDone.UI.ViewModels;

namespace DotDone.UI;

public partial class App : Application
{
    private Window? _window;
    public static MainWindow? MainWindowInstance { get; private set; }
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Ensure database exists and seed initial data if empty
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DotDoneDbContext>();
        db.Database.EnsureCreated();
        DbInitializer.Initialize(db);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "DotDone_Data");
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        var dbPath = Path.Combine(dataDir, "dotdone.db");
        services.AddDbContext<DotDoneDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });

        services.AddSingleton<IFileStorageService>(_ => new FileStorageService());
        services.AddScoped<IStreakService, StreakService>();
        services.AddScoped<ITaskTransferService, TaskTransferService>();
        services.AddScoped<IActivityHeatmapService, ActivityHeatmapService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddScoped<ITaskService, TaskService>();

        services.AddTransient<MainViewModel>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var main = new MainWindow();
        _window = main;
        MainWindowInstance = main;
        _window.Activate();

        if (main.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
    }
}
