using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using DotDone.Services;

namespace DotDone.UI.Controls;

public sealed partial class HeatmapControl : UserControl
{
    public static readonly DependencyProperty HeatmapDataProperty =
        DependencyProperty.Register(nameof(HeatmapData), typeof(HeatmapSummaryDto), typeof(HeatmapControl),
            new PropertyMetadata(null, OnDataChanged));

    public static readonly DependencyProperty LocalizationProperty =
        DependencyProperty.Register(nameof(Localization), typeof(ILocalizationService), typeof(HeatmapControl),
            new PropertyMetadata(null, OnDataChanged));

    public static readonly DependencyProperty CurrentLanguageProperty =
        DependencyProperty.Register(nameof(CurrentLanguage), typeof(DotDone.Core.Enums.AppLanguage), typeof(HeatmapControl),
            new PropertyMetadata(DotDone.Core.Enums.AppLanguage.Persian, OnDataChanged));

    public HeatmapSummaryDto? HeatmapData
    {
        get => (HeatmapSummaryDto?)GetValue(HeatmapDataProperty);
        set => SetValue(HeatmapDataProperty, value);
    }

    public ILocalizationService? Localization
    {
        get => (ILocalizationService?)GetValue(LocalizationProperty);
        set => SetValue(LocalizationProperty, value);
    }

    public DotDone.Core.Enums.AppLanguage CurrentLanguage
    {
        get => (DotDone.Core.Enums.AppLanguage)GetValue(CurrentLanguageProperty);
        set => SetValue(CurrentLanguageProperty, value);
    }

    private static readonly SolidColorBrush Level0Brush = new(Windows.UI.Color.FromArgb(255, 40, 45, 53));
    private static readonly SolidColorBrush Level1Brush = new(Windows.UI.Color.FromArgb(255, 18, 138, 64));
    private static readonly SolidColorBrush Level2Brush = new(Windows.UI.Color.FromArgb(255, 38, 178, 74));
    private static readonly SolidColorBrush Level3Brush = new(Windows.UI.Color.FromArgb(255, 57, 218, 92));
    private static readonly SolidColorBrush Level4Brush = new(Windows.UI.Color.FromArgb(255, 140, 240, 155));

    public HeatmapControl()
    {
        InitializeComponent();
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HeatmapControl control)
        {
            control.RenderSquares();
        }
    }

    private void RenderSquares()
    {
        HeatmapCanvas.Children.Clear();
        if (HeatmapData == null || HeatmapData.Days.Count == 0) return;

        bool isPersian = (Localization?.CurrentLanguage ?? CurrentLanguage) == DotDone.Core.Enums.AppLanguage.Persian;
        TxtLess.Text = Localization?.GetString("Less") ?? (isPersian ? "کمتر" : "Less");
        TxtMore.Text = Localization?.GetString("More") ?? (isPersian ? "بیشتر" : "More");

        const double cellSize = 14;
        const double spacing = 3.5;
        const double step = cellSize + spacing;

        var days = HeatmapData.Days;
        int totalDays = days.Count;

        // Calculate columns (weeks)
        int numWeeks = (int)Math.Ceiling(totalDays / 7.0);
        HeatmapCanvas.Height = 7 * step;
        HeatmapCanvas.Width = Math.Max(920, (numWeeks + 1) * step);

        for (int i = 0; i < totalDays; i++)
        {
            var day = days[i];
            int col = i / 7;
            int row = i % 7;

            var rect = new Rectangle
            {
                Width = cellSize,
                Height = cellSize,
                RadiusX = 3,
                RadiusY = 3,
                Fill = GetBrush(day.IntensityLevel)
            };

            Canvas.SetLeft(rect, col * step);
            Canvas.SetTop(rect, row * step);

            // Tooltip
            string dateStr = Localization != null ? Localization.FormatDate(day.Date) : day.Date.ToString();
            string tooltip = isPersian
                ? $"{dateStr}\nفعالیت: {day.TotalWeight} امتیاز\nانجام شده: {day.DoneCount}\nدرحال انجام: {day.InProgressCount}"
                : $"{dateStr}\nScore: {day.TotalWeight}\nDone: {day.DoneCount}\nIn Progress: {day.InProgressCount}";

            ToolTipService.SetToolTip(rect, tooltip);
            HeatmapCanvas.Children.Add(rect);
        }
    }

    private static SolidColorBrush GetBrush(int intensity)
    {
        return intensity switch
        {
            1 => Level1Brush,
            2 => Level2Brush,
            3 => Level3Brush,
            4 => Level4Brush,
            _ => Level0Brush
        };
    }
}
