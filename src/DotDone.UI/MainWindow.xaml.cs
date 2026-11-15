using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace DotDone.UI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "DotDone";

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
        }

        CenterOnScreen();

        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }

        RootMainPage.SetParentWindow(this);
    }

    private void CenterOnScreen()
    {
        var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
        if (displayArea != null)
        {
            var workArea = displayArea.WorkArea;
            var width = Math.Min(AppWindow.Size.Width > 0 ? AppWindow.Size.Width : 1200, workArea.Width);
            var height = Math.Min(AppWindow.Size.Height > 0 ? AppWindow.Size.Height : 800, workArea.Height);
            var x = workArea.X + (workArea.Width - width) / 2;
            var y = workArea.Y + (workArea.Height - height) / 2;
            AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
        }
    }
}
