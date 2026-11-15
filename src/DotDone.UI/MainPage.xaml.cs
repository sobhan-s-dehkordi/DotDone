using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using DotDone.Core.Enums;
using DotDone.Data.Services;
using DotDone.Services;
using DotDone.UI.Dialogs;
using DotDone.UI.ViewModels;

namespace DotDone.UI;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }
    private readonly ITaskService _taskService;
    private readonly IFileStorageService _fileStorage;
    private readonly ILocalizationService _localization;
    private Window? _parentWindow;

    public MainPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        _taskService = App.Services.GetRequiredService<ITaskService>();
        _fileStorage = App.Services.GetRequiredService<IFileStorageService>();
        _localization = App.Services.GetRequiredService<ILocalizationService>();

        UpdateFilterLabel();
        RequestedTheme = ViewModel.CurrentTheme == AppTheme.Dark
            ? ElementTheme.Dark
            : ElementTheme.Light;

        Loaded += async (s, e) =>
        {
            await ViewModel.InitializeAsync();
            await CheckForStreakGapsAsync();
        };
    }

    public void SetParentWindow(Window window)
    {
        _parentWindow = window;
        if (window.Content is FrameworkElement fe)
        {
            fe.RequestedTheme = RequestedTheme;
        }
    }

    private Window GetWindow() => _parentWindow ?? App.MainWindowInstance!;

    private async Task CheckForStreakGapsAsync()
    {
        var gap = await ViewModel.CheckActiveRepoStreakGapAsync();
        if (gap != null && gap.HasGap && gap.MissedDays.Count > 0)
        {
            var eligibleItems = ViewModel.SelectedRepository?.Projects
                .SelectMany(p => p.HeadTasks.Select(t => (t.Id, "HeadTask", t.Title)))
                .ToList() ?? new();

            var gapVm = new StreakGapDialogViewModel(
                gap,
                eligibleItems,
                App.Services.GetRequiredService<IStreakService>(),
                _localization
            );

            var dialog = new StreakGapDialog(gapVm, GetWindow());
            await dialog.ShowAsync();
            await ViewModel.RefreshHeatmapAsync();
        }
    }

    private void UpdateFilterLabel()
    {
        Bindings.Update();
    }

    private async void ToggleHeatmap_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ToggleHeatmapFilterAsync();
        Bindings.Update();
    }

    private void ToggleLanguage_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleLanguage();
        Bindings.Update();
    }

    private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleTheme();
        RequestedTheme = ViewModel.CurrentTheme == AppTheme.Dark
            ? ElementTheme.Dark
            : ElementTheme.Light;

        if (GetWindow().Content is FrameworkElement root)
        {
            root.RequestedTheme = RequestedTheme;
        }
    }

    private async void ShowAboutDialog_Click(object sender, RoutedEventArgs e)
    {
        var isRtl = _localization.IsPersian;
        var flowDirection = isRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        var container = new StackPanel
        {
            Spacing = 16,
            Width = 360,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Header: Logo + App Name + Subtitle
        var headerStack = new StackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var logoBorder = new Border
        {
            Width = 56,
            Height = 56,
            CornerRadius = new CornerRadius(12),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var logoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");
        if (System.IO.File.Exists(logoPath))
        {
            logoBorder.Child = new Image
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(logoPath)),
                Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
            };
        }

        var appTitle = new TextBlock
        {
            Text = "DotDone",
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var appSubtitle = new TextBlock
        {
            Text = _localization.GetString("AppSubtitle"),
            FontSize = 13,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            HorizontalAlignment = HorizontalAlignment.Center
        };

        headerStack.Children.Add(logoBorder);
        headerStack.Children.Add(appTitle);
        headerStack.Children.Add(appSubtitle);
        container.Children.Add(headerStack);

        // Metadata Card
        var card = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12)
        };

        var cardStack = new StackPanel { Spacing = 12 };

        Grid CreateMetaRow(string glyph, string label, string value, bool isBadge = false)
        {
            var rowGrid = new Grid();
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var icon = new FontIcon
            {
                Glyph = glyph,
                FontSize = 14,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"],
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(icon, 0);

            var labelBlock = new TextBlock
            {
                Text = label,
                FontSize = 13,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelBlock, 1);

            FrameworkElement valElement;
            if (isBadge)
            {
                valElement = new Border
                {
                    Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentFillColorDefaultBrush"],
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 2, 8, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = value,
                        FontSize = 12,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"],
                        FlowDirection = FlowDirection.LeftToRight
                    }
                };
            }
            else
            {
                valElement = new TextBlock
                {
                    Text = value,
                    FontSize = 13,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FlowDirection = FlowDirection.LeftToRight,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            Grid.SetColumn(valElement, 2);

            rowGrid.Children.Add(icon);
            rowGrid.Children.Add(labelBlock);
            rowGrid.Children.Add(valElement);
            return rowGrid;
        }

        cardStack.Children.Add(CreateMetaRow("\uE77B", ViewModel.L_ProducerLabel, ViewModel.AppProducer));
        cardStack.Children.Add(new Border { Height = 1, Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"] });
        cardStack.Children.Add(CreateMetaRow("\uE787", ViewModel.L_ReleaseDateLabel, ViewModel.AppReleaseDate));
        cardStack.Children.Add(new Border { Height = 1, Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"] });
        cardStack.Children.Add(CreateMetaRow("\uE946", ViewModel.L_VersionLabel, ViewModel.AppVersion, isBadge: true));

        card.Child = cardStack;
        container.Children.Add(card);

        var dialog = CreateStyledDialog(
            ViewModel.L_AboutDotDone,
            container,
            primaryText: null,
            closeText: ViewModel.L_Close,
            defaultBtn: ContentDialogButton.Close
        );

        await dialog.ShowAsync();
    }

    #region Global Search
    private async void GlobalSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            await ViewModel.PerformSearchAsync(sender.Text);
            sender.ItemsSource = ViewModel.SearchResults;
        }
    }

    private void GlobalSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is SearchResultItemDto item)
        {
            ViewModel.SelectSearchResult(item);
        }
    }
    #endregion

    #region Repository Management
    private void RepoItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RepositoryViewModel repo)
        {
            ViewModel.SelectedRepository = repo;
            RepoSelectDropDown.Flyout?.Hide();
        }
    }

    #region Dialog UI Helpers
    private ContentDialog CreateStyledDialog(string title, FrameworkElement content, string? primaryText = null, string? closeText = null, ContentDialogButton defaultBtn = ContentDialogButton.Primary)
    {
        var isRtl = _localization.IsPersian;
        var flowDirection = isRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var appFont = (Microsoft.UI.Xaml.Media.FontFamily)Application.Current.Resources["AppFontFamily"];

        content.HorizontalAlignment = HorizontalAlignment.Center;
        content.FlowDirection = flowDirection;

        FrameworkElement dialogContent;
        if (content is ScrollViewer sv)
        {
            sv.HorizontalAlignment = HorizontalAlignment.Center;
            sv.FlowDirection = flowDirection;
            dialogContent = sv;
        }
        else
        {
            dialogContent = new ScrollViewer
            {
                Content = content,
                MaxHeight = 440,
                HorizontalAlignment = HorizontalAlignment.Center,
                FlowDirection = flowDirection,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
        }

        var xamlRoot = XamlRoot ?? App.MainWindowInstance?.Content?.XamlRoot;

        var titleBlock = new TextBlock
        {
            Text = title,
            FlowDirection = flowDirection,
            FontFamily = appFont,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 18,
            TextAlignment = isRtl ? TextAlignment.Right : TextAlignment.Left,
            HorizontalTextAlignment = isRtl ? TextAlignment.Right : TextAlignment.Left,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var dialog = new ContentDialog
        {
            Title = titleBlock,
            Content = dialogContent,
            FlowDirection = flowDirection,
            RequestedTheme = RequestedTheme,
            FontFamily = appFont,
            XamlRoot = xamlRoot,
            CornerRadius = new CornerRadius(12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        if (Application.Current.Resources.TryGetValue("DefaultContentDialogStyle", out var styleObj) && styleObj is Style style)
        {
            dialog.Style = style;
        }

        if (!string.IsNullOrEmpty(primaryText))
        {
            dialog.PrimaryButtonText = primaryText;
        }

        if (!string.IsNullOrEmpty(closeText))
        {
            dialog.CloseButtonText = closeText;
        }

        dialog.DefaultButton = defaultBtn;
        return dialog;
    }

    private FrameworkElement CreateConfirmationContent(string message, string glyph, Windows.UI.Color iconColor)
    {
        var isRtl = _localization.IsPersian;
        var flowDirection = isRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var appFont = (Microsoft.UI.Xaml.Media.FontFamily)Application.Current.Resources["AppFontFamily"];

        if (isRtl && !message.StartsWith("\u200F"))
        {
            message = "\u200F" + message;
        }

        var border = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorAltBrush"],
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16, 14, 16, 14),
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = 460,
            FlowDirection = flowDirection
        };

        var grid = new Grid
        {
            ColumnSpacing = 12,
            FlowDirection = flowDirection,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icon = new FontIcon
        {
            Glyph = glyph,
            FontSize = 22,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(iconColor),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);

        var textBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13.5,
            LineHeight = 22,
            VerticalAlignment = VerticalAlignment.Center,
            FlowDirection = flowDirection,
            FontFamily = appFont,
            TextAlignment = isRtl ? TextAlignment.Right : TextAlignment.Left,
            HorizontalTextAlignment = isRtl ? TextAlignment.Right : TextAlignment.Left,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetColumn(textBlock, 1);
        grid.Children.Add(textBlock);

        border.Child = grid;
        return border;
    }

    private Border CreateMetricTile(string glyph, string label, string value, Windows.UI.Color? accentColor = null)
    {
        var border = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 10)
        };

        var sp = new StackPanel { Spacing = 4 };
        var topSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };

        var fontBrush = accentColor.HasValue
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(accentColor.Value)
            : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"];

        topSp.Children.Add(new FontIcon { Glyph = glyph, FontSize = 12, Foreground = fontBrush });
        topSp.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });
        sp.Children.Add(topSp);

        var valBlock = new TextBlock
        {
            Text = value,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        };
        if (accentColor.HasValue)
        {
            valBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(accentColor.Value);
        }
        sp.Children.Add(valBlock);

        border.Child = sp;
        return border;
    }
    #endregion

    private async void AddRepository_Click(object sender, RoutedEventArgs e)
    {
        var titleInput = new TextBox
        {
            Header = _localization.GetString("Title"),
            PlaceholderText = _localization.GetString("NewRepoPlaceholder"),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var descInput = new TextBox
        {
            Header = _localization.GetString("Description"),
            PlaceholderText = _localization.GetString("DescriptionPlaceholder"),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 75,
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var streakCheck = new CheckBox
        {
            Content = $"🔥 {_localization.GetString("EnableStreakCheckbox")}",
            IsChecked = true
        };

        var panel = new StackPanel { Spacing = 2, Width = 440, HorizontalAlignment = HorizontalAlignment.Center };
        panel.Children.Add(titleInput);
        panel.Children.Add(descInput);
        panel.Children.Add(streakCheck);

        var dialog = CreateStyledDialog(
            _localization.GetString("NewRepoDialogTitle"),
            panel,
            _localization.GetString("Create"),
            _localization.GetString("Cancel"));

        if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(titleInput.Text))
        {
            await ViewModel.CreateRepositoryAsync((titleInput.Text, descInput.Text, streakCheck.IsChecked == true));
        }
    }

    private async void EditRepository_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRepository == null) return;
        var repo = ViewModel.SelectedRepository;

        var titleInput = new TextBox
        {
            Header = _localization.GetString("Title"),
            Text = repo.Title,
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var descInput = new TextBox
        {
            Header = _localization.GetString("Description"),
            Text = repo.Description ?? string.Empty,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 75,
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var streakCheck = new CheckBox
        {
            Content = $"🔥 {_localization.GetString("EnableStreakCheckbox")}",
            IsChecked = repo.IsStreakEnabled
        };

        var panel = new StackPanel { Spacing = 2, Width = 440, HorizontalAlignment = HorizontalAlignment.Center };
        panel.Children.Add(titleInput);
        panel.Children.Add(descInput);
        panel.Children.Add(streakCheck);

        var dialog = CreateStyledDialog(
            _localization.GetString("EditRepoDialogTitle"),
            panel,
            _localization.GetString("Save"),
            _localization.GetString("Cancel"));

        if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(titleInput.Text))
        {
            var message = string.Format(_localization.GetString("EditConfirmMessageFormat"), repo.Title);
            var content = CreateConfirmationContent(message, "\uE70F", Windows.UI.Color.FromArgb(255, 0, 120, 212));

            var confirm = CreateStyledDialog(
                _localization.GetString("EditConfirmTitle"),
                content,
                _localization.GetString("YesSave"),
                _localization.GetString("Cancel"),
                ContentDialogButton.Primary);

            if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.UpdateRepositoryAsync((repo.Id, titleInput.Text, descInput.Text, streakCheck.IsChecked == true));
            }
        }
    }

    private async void ViewRepository_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRepository == null) return;
        var repo = ViewModel.SelectedRepository;

        var panel = new StackPanel { Spacing = 14, Width = 460, HorizontalAlignment = HorizontalAlignment.Center };

        // Header Card with Title and Description
        var topCard = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorAltBrush"],
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 12, 14, 12)
        };
        var topSp = new StackPanel { Spacing = 6 };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        titleRow.Children.Add(new FontIcon { Glyph = "\uE8F1", FontSize = 18, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"] });
        titleRow.Children.Add(new TextBlock { Text = repo.Title, FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center });
        topSp.Children.Add(titleRow);

        if (!string.IsNullOrWhiteSpace(repo.Description))
        {
            topSp.Children.Add(new TextBlock
            {
                Text = repo.Description,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
        }
        topCard.Child = topSp;
        panel.Children.Add(topCard);

        // 2x2 Metric Tiles
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var projNum = _localization.ToLocalDigits(repo.TotalProjectsCount.ToString());
        var taskNum = _localization.ToLocalDigits(repo.TotalTasksCount.ToString());
        var streakState = repo.IsStreakEnabled ? _localization.GetString("Enabled") : _localization.GetString("Disabled");
        var streakNum = _localization.ToLocalDigits(repo.CurrentStreak.ToString());

        var tile1 = CreateMetricTile("\uE82D", _localization.GetString("TotalProjects"), projNum);
        Grid.SetColumn(tile1, 0); Grid.SetRow(tile1, 0); grid.Children.Add(tile1);

        var tile2 = CreateMetricTile("\uE762", _localization.GetString("TotalTasks"), taskNum);
        Grid.SetColumn(tile2, 1); Grid.SetRow(tile2, 0); grid.Children.Add(tile2);

        var tile3 = CreateMetricTile("🔥", _localization.GetString("StreakStatus"), streakState, repo.IsStreakEnabled ? Windows.UI.Color.FromArgb(255, 255, 140, 0) : null);
        Grid.SetColumn(tile3, 0); Grid.SetRow(tile3, 1); grid.Children.Add(tile3);

        var tile4 = CreateMetricTile("📅", _localization.GetString("StreakBadge"), repo.IsStreakEnabled ? streakNum : "-");
        Grid.SetColumn(tile4, 1); Grid.SetRow(tile4, 1); grid.Children.Add(tile4);

        panel.Children.Add(grid);

        var dialog = CreateStyledDialog(
            _localization.GetString("RepoDetailsDialogTitle"),
            panel,
            null,
            _localization.GetString("GotIt"),
            ContentDialogButton.Close);

        await dialog.ShowAsync();
    }

    private async void DeleteRepository_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRepository == null) return;
        var repo = ViewModel.SelectedRepository;

        var message = string.Format(_localization.GetString("DeleteRepoConfirmMessageFormat"), repo.Title);
        var content = CreateConfirmationContent(message, "\uE7BA", Windows.UI.Color.FromArgb(255, 220, 53, 69));

        var confirm = CreateStyledDialog(
            _localization.GetString("DeleteRepoConfirmTitle"),
            content,
            _localization.GetString("YesDelete"),
            _localization.GetString("Cancel"),
            ContentDialogButton.Close);

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteRepositoryAsync(repo);
        }
    }
    #endregion

    #region Add Project & Task Handlers
    private async void AddProject_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRepository == null) return;

        var titleInput = new TextBox
        {
            Header = _localization.GetString("Title"),
            PlaceholderText = _localization.GetString("NewProjectPlaceholder"),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var descInput = new TextBox
        {
            Header = _localization.GetString("Description"),
            PlaceholderText = _localization.GetString("DescriptionPlaceholder"),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 70,
            CornerRadius = new CornerRadius(6)
        };

        var panel = new StackPanel { Spacing = 2, Width = 440, HorizontalAlignment = HorizontalAlignment.Center };
        panel.Children.Add(titleInput);
        panel.Children.Add(descInput);

        var dialog = CreateStyledDialog(
            _localization.GetString("NewProjectDialogTitle"),
            panel,
            _localization.GetString("Create"),
            _localization.GetString("Cancel"));

        if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(titleInput.Text))
        {
            await ViewModel.CreateProjectAsync(titleInput.Text);
        }
    }

    private async void ViewProject_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProject == null) return;
        var prj = ViewModel.SelectedProject;

        var panel = new StackPanel { Spacing = 14, Width = 460, HorizontalAlignment = HorizontalAlignment.Center };

        // Top Card
        var topCard = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorAltBrush"],
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 12, 14, 12)
        };
        var topSp = new StackPanel { Spacing = 6 };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        titleRow.Children.Add(new FontIcon { Glyph = "\uE82D", FontSize = 18, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"] });
        titleRow.Children.Add(new TextBlock { Text = prj.Title, FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center });
        topSp.Children.Add(titleRow);

        if (!string.IsNullOrWhiteSpace(prj.Description))
        {
            topSp.Children.Add(new TextBlock
            {
                Text = prj.Description,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
        }
        topCard.Child = topSp;
        panel.Children.Add(topCard);

        // 2x2 Metric Tiles
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var totalHeadNum = _localization.ToLocalDigits(prj.TotalHeadTasksCount.ToString());
        var totalSubNum = _localization.ToLocalDigits(prj.TotalSubTasksCount.ToString());
        var doneNum = _localization.ToLocalDigits(prj.DoneHeadTasksCount.ToString());
        var inProgNum = _localization.ToLocalDigits(prj.InProgressHeadTasksCount.ToString());

        var tile1 = CreateMetricTile("\uE762", _localization.GetString("TotalTasks"), totalHeadNum);
        Grid.SetColumn(tile1, 0); Grid.SetRow(tile1, 0); grid.Children.Add(tile1);

        var tile2 = CreateMetricTile("\uE8A5", _localization.GetString("SubtasksBadge"), totalSubNum);
        Grid.SetColumn(tile2, 1); Grid.SetRow(tile2, 0); grid.Children.Add(tile2);

        var tile3 = CreateMetricTile("\uE73E", _localization.GetString("CompletedTasks"), doneNum, Windows.UI.Color.FromArgb(255, 46, 160, 67));
        Grid.SetColumn(tile3, 0); Grid.SetRow(tile3, 1); grid.Children.Add(tile3);

        var tile4 = CreateMetricTile("\uE895", _localization.GetString("InProgressTasks"), inProgNum, Windows.UI.Color.FromArgb(255, 255, 170, 0));
        Grid.SetColumn(tile4, 1); Grid.SetRow(tile4, 1); grid.Children.Add(tile4);

        panel.Children.Add(grid);

        var dialog = CreateStyledDialog(
            _localization.GetString("ProjectSummaryDialogTitle"),
            panel,
            null,
            _localization.GetString("GotIt"),
            ContentDialogButton.Close);

        await dialog.ShowAsync();
    }

    private async void DeleteSelectedProject_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProject == null) return;
        var project = ViewModel.SelectedProject;

        var message = string.Format(_localization.GetString("DeleteProjectConfirmMessageFormat"), project.Title);
        var content = CreateConfirmationContent(message, "\uE7BA", Windows.UI.Color.FromArgb(255, 220, 53, 69));

        var confirm = CreateStyledDialog(
            _localization.GetString("DeleteProjectConfirmTitle"),
            content,
            _localization.GetString("YesDelete"),
            _localization.GetString("Cancel"),
            ContentDialogButton.Close);

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteProjectAsync(project);
        }
    }

    private async void NewTaskTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && !string.IsNullOrWhiteSpace(NewTaskTextBox.Text))
        {
            var text = NewTaskTextBox.Text;
            NewTaskTextBox.Text = string.Empty;
            await ViewModel.CreateHeadTaskAsync(text);
        }
    }

    private async void NewSubTaskTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && sender is TextBox tb && tb.Tag is HeadTaskViewModel parent && !string.IsNullOrWhiteSpace(tb.Text))
        {
            var text = tb.Text;
            tb.Text = string.Empty;
            await ViewModel.CreateSubTaskAsync((parent, text));
        }
    }
    #endregion

    #region Status Toggles
    private async void CycleHeadTaskStatus_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: HeadTaskViewModel task })
        {
            var nextStatus = task.Status switch
            {
                ItemStatus.Todo => ItemStatus.InProgress,
                ItemStatus.InProgress => ItemStatus.Done,
                ItemStatus.Done => ItemStatus.Todo,
                _ => ItemStatus.Todo
            };

            var nextStatusText = _localization.GetStatusText(nextStatus);
            var message = string.Format(_localization.GetString("ChangeStatusConfirmMessageFormat"), task.Title, nextStatusText);
            var content = CreateConfirmationContent(message, "\uE8AB", Windows.UI.Color.FromArgb(255, 0, 120, 212));

            var confirm = CreateStyledDialog(
                _localization.GetString("ChangeStatusConfirmTitle"),
                content,
                _localization.GetString("YesChange"),
                _localization.GetString("Cancel"),
                ContentDialogButton.Primary);

            if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.UpdateHeadTaskStatusAsync((task, nextStatus));
            }
        }
    }

    private async void CycleSubTaskStatus_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SubTaskViewModel sub } && ViewModel.SelectedProject != null)
        {
            var parent = ViewModel.SelectedProject.HeadTasks.FirstOrDefault(t => t.Id == sub.HeadTaskId);
            if (parent != null)
            {
                var nextStatus = sub.Status switch
                {
                    ItemStatus.Todo => ItemStatus.InProgress,
                    ItemStatus.InProgress => ItemStatus.Done,
                    ItemStatus.Done => ItemStatus.Todo,
                    _ => ItemStatus.Todo
                };

                var nextStatusText = _localization.GetStatusText(nextStatus);
                var message = string.Format(_localization.GetString("ChangeStatusConfirmMessageFormat"), sub.Title, nextStatusText);
                var content = CreateConfirmationContent(message, "\uE8AB", Windows.UI.Color.FromArgb(255, 0, 120, 212));

                var confirm = CreateStyledDialog(
                    _localization.GetString("ChangeStatusConfirmTitle"),
                    content,
                    _localization.GetString("YesChange"),
                    _localization.GetString("Cancel"),
                    ContentDialogButton.Primary);

                if (await confirm.ShowAsync() == ContentDialogResult.Primary)
                {
                    await ViewModel.UpdateSubTaskStatusAsync((sub, parent, nextStatus));
                }
            }
        }
    }

    private void ToggleSubTasksExpand_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: HeadTaskViewModel task })
        {
            task.IsExpanded = !task.IsExpanded;
        }
    }
    #endregion

    #region Task Actions Toolbar
    private async void AddTask_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProject == null) return;

        var titleInput = new TextBox
        {
            Header = _localization.GetString("Title"),
            PlaceholderText = _localization.GetString("NewTaskPlaceholder"),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var descInput = new TextBox
        {
            Header = _localization.GetString("Description"),
            PlaceholderText = _localization.GetString("DescriptionPlaceholder"),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 70,
            CornerRadius = new CornerRadius(6)
        };

        var panel = new StackPanel { Spacing = 2, Width = 440, HorizontalAlignment = HorizontalAlignment.Center };
        panel.Children.Add(titleInput);
        panel.Children.Add(descInput);

        var dialog = CreateStyledDialog(
            _localization.GetString("NewTaskDialogTitle"),
            panel,
            _localization.GetString("Create"),
            _localization.GetString("Cancel"));

        if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(titleInput.Text))
        {
            await ViewModel.CreateHeadTaskWithDetailsAsync((titleInput.Text, descInput.Text));
        }
    }

    private async void EditSelectedTask_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedHeadTask == null)
        {
            await ShowNoTaskSelectedPromptAsync();
            return;
        }

        await OpenHeadTaskDetailAsync(ViewModel.SelectedHeadTask);
    }

    private async void ViewSelectedTask_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedHeadTask == null)
        {
            await ShowNoTaskSelectedPromptAsync();
            return;
        }

        await ShowHeadTaskSummaryAsync(ViewModel.SelectedHeadTask);
    }

    private async void DeleteSelectedTask_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedHeadTask == null)
        {
            await ShowNoTaskSelectedPromptAsync();
            return;
        }

        var task = ViewModel.SelectedHeadTask;
        var message = string.Format(_localization.GetString("DeleteTaskConfirmMessageFormat"), task.Title);
        var content = CreateConfirmationContent(message, "\uE7BA", Windows.UI.Color.FromArgb(255, 220, 53, 69));

        var confirm = CreateStyledDialog(
            _localization.GetString("DeleteTaskConfirmTitle"),
            content,
            _localization.GetString("YesDelete"),
            _localization.GetString("Cancel"),
            ContentDialogButton.Close);

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteHeadTaskAsync(task);
        }
    }

    private void HeadTaskCard_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: HeadTaskViewModel task })
        {
            ViewModel.SelectedHeadTask = task;
        }
    }

    private async Task ShowNoTaskSelectedPromptAsync()
    {
        var content = CreateConfirmationContent(_localization.GetString("NoTaskSelectedPrompt"), "\uE946", Windows.UI.Color.FromArgb(255, 0, 120, 212));

        var prompt = CreateStyledDialog(
            _localization.GetString("TaskDetailsTitle"),
            content,
            null,
            _localization.GetString("GotIt"),
            ContentDialogButton.Close);

        await prompt.ShowAsync();
    }
    #endregion

    #region Project & Task Details Dialogs
    private async void EditProject_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProject != null)
        {
            var prj = ViewModel.SelectedProject;
            var vm = new DetailDialogViewModel(
                prj.Id,
                ItemType.Project,
                prj.Title,
                prj.Description,
                prj.Status,
                prj.ReviewNotes,
                prj.CreatedDate,
                prj.ModifiedDate,
                prj.Attachments,
                _taskService,
                _fileStorage,
                _localization
            );

            var dialog = new TaskDetailDialog(vm, GetWindow());
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var message = string.Format(_localization.GetString("EditConfirmMessageFormat"), prj.Title);
                var content = CreateConfirmationContent(message, "\uE70F", Windows.UI.Color.FromArgb(255, 0, 120, 212));

                var confirm = CreateStyledDialog(
                    _localization.GetString("EditConfirmTitle"),
                    content,
                    _localization.GetString("YesSave"),
                    _localization.GetString("Cancel"),
                    ContentDialogButton.Primary);

                if (await confirm.ShowAsync() == ContentDialogResult.Primary)
                {
                    await vm.SaveChangesAsync();
                    prj.Title = vm.Title;
                    prj.Description = vm.Description;
                    prj.Status = vm.Status;
                    prj.ReviewNotes = vm.ReviewNotes;
                    await ViewModel.RefreshHeatmapAsync();
                }
            }
        }
    }

    private async Task OpenHeadTaskDetailAsync(HeadTaskViewModel task)
    {
        var vm = new DetailDialogViewModel(
            task.Id,
            ItemType.HeadTask,
            task.Title,
            task.Description,
            task.Status,
            task.ReviewNotes,
            task.CreatedDate,
            task.ModifiedDate,
            task.Attachments,
            _taskService,
            _fileStorage,
            _localization
        );

        var dialog = new TaskDetailDialog(vm, GetWindow());
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var message = string.Format(_localization.GetString("EditConfirmMessageFormat"), task.Title);
            var content = CreateConfirmationContent(message, "\uE70F", Windows.UI.Color.FromArgb(255, 0, 120, 212));

            var confirm = CreateStyledDialog(
                _localization.GetString("EditConfirmTitle"),
                content,
                _localization.GetString("YesSave"),
                _localization.GetString("Cancel"),
                ContentDialogButton.Primary);

            if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            {
                await vm.SaveChangesAsync();
                task.Title = vm.Title;
                task.Description = vm.Description;
                task.Status = vm.Status;
                task.ReviewNotes = vm.ReviewNotes;
                await ViewModel.RefreshHeatmapAsync();
            }
        }
    }

    private async Task OpenSubTaskDetailAsync(SubTaskViewModel sub)
    {
        var vm = new DetailDialogViewModel(
            sub.Id,
            ItemType.SubTask,
            sub.Title,
            sub.Description,
            sub.Status,
            sub.ReviewNotes,
            sub.CreatedDate,
            sub.ModifiedDate,
            sub.Attachments,
            _taskService,
            _fileStorage,
            _localization
        );

        var dialog = new TaskDetailDialog(vm, GetWindow());
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var message = string.Format(_localization.GetString("EditConfirmMessageFormat"), sub.Title);
            var content = CreateConfirmationContent(message, "\uE70F", Windows.UI.Color.FromArgb(255, 0, 120, 212));

            var confirm = CreateStyledDialog(
                _localization.GetString("EditConfirmTitle"),
                content,
                _localization.GetString("YesSave"),
                _localization.GetString("Cancel"),
                ContentDialogButton.Primary);

            if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            {
                await vm.SaveChangesAsync();
                sub.Title = vm.Title;
                sub.Description = vm.Description;
                sub.Status = vm.Status;
                sub.ReviewNotes = vm.ReviewNotes;
                var parent = ViewModel.SelectedProject?.HeadTasks.FirstOrDefault(t => t.Id == sub.HeadTaskId);
                parent?.NotifyProgressChanged();
                await ViewModel.RefreshHeatmapAsync();
            }
        }
    }

    private async Task ShowHeadTaskSummaryAsync(HeadTaskViewModel task)
    {
        var panel = new StackPanel { Spacing = 12, Width = 460, HorizontalAlignment = HorizontalAlignment.Center };

        // Top Card
        var topCard = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorAltBrush"],
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 12, 14, 12)
        };
        var topSp = new StackPanel { Spacing = 8 };

        var titleRow = new Grid();
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleBlock = new TextBlock
        {
            Text = task.Title,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(titleBlock, 0);
        titleRow.Children.Add(titleBlock);

        var statusColor = task.Status switch
        {
            ItemStatus.Done => Windows.UI.Color.FromArgb(255, 46, 160, 67),
            ItemStatus.InProgress => Windows.UI.Color.FromArgb(255, 255, 170, 0),
            _ => Windows.UI.Color.FromArgb(255, 130, 130, 130)
        };
        var statusBadge = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor) { Opacity = 0.15 },
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10, 2, 10, 2),
            VerticalAlignment = VerticalAlignment.Center
        };
        statusBadge.Child = new TextBlock
        {
            Text = _localization.GetStatusText(task.Status),
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor)
        };
        Grid.SetColumn(statusBadge, 1);
        titleRow.Children.Add(statusBadge);

        topSp.Children.Add(titleRow);

        if (!string.IsNullOrWhiteSpace(task.Description))
        {
            topSp.Children.Add(new TextBlock
            {
                Text = task.Description,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
        }
        topCard.Child = topSp;
        panel.Children.Add(topCard);

        // Metric Tiles
        var grid = new Grid { ColumnSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var totalSubs = _localization.ToLocalDigits(task.TotalSubTasksCount.ToString());
        var doneSubs = _localization.ToLocalDigits(task.CompletedSubTasksCount.ToString());
        var attachCount = _localization.ToLocalDigits(task.Attachments.Count.ToString());

        var tile1 = CreateMetricTile("\uE8A5", _localization.GetString("SubtasksBadge"), $"{doneSubs}/{totalSubs}");
        Grid.SetColumn(tile1, 0); grid.Children.Add(tile1);

        var tile2 = CreateMetricTile("\uE8A5", _localization.GetString("Attachments"), attachCount);
        Grid.SetColumn(tile2, 1); grid.Children.Add(tile2);

        panel.Children.Add(grid);

        // Review notes card (if any)
        if (!string.IsNullOrWhiteSpace(task.ReviewNotes))
        {
            var notesCard = new Border
            {
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10)
            };
            var notesSp = new StackPanel { Spacing = 4 };
            notesSp.Children.Add(new TextBlock
            {
                Text = _localization.GetString("ReviewNotes"),
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            notesSp.Children.Add(new TextBlock
            {
                Text = task.ReviewNotes,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            notesCard.Child = notesSp;
            panel.Children.Add(notesCard);
        }

        // Dates footer
        var footer = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorAltBrush"],
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8, 12, 8)
        };
        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var createdFormatted = _localization.FormatDate(task.CreatedDate);
        var createdBlock = new TextBlock { Text = $"{_localization.GetString("CreatedDateLabel")} {createdFormatted}", FontSize = 11, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] };
        Grid.SetColumn(createdBlock, 0);
        footerGrid.Children.Add(createdBlock);

        if (task.ModifiedDate.HasValue)
        {
            var modifiedFormatted = _localization.FormatDate(task.ModifiedDate.Value);
            var modBlock = new TextBlock { Text = $"{_localization.GetString("ModifiedDateLabel")} {modifiedFormatted}", FontSize = 11, HorizontalAlignment = HorizontalAlignment.Right, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] };
            Grid.SetColumn(modBlock, 1);
            footerGrid.Children.Add(modBlock);
        }
        footer.Child = footerGrid;
        panel.Children.Add(footer);

        var dialog = CreateStyledDialog(
            _localization.GetString("TaskSummaryDialogTitle"),
            panel,
            null,
            _localization.GetString("GotIt"),
            ContentDialogButton.Close);

        await dialog.ShowAsync();
    }

    private async Task ShowSubTaskSummaryAsync(SubTaskViewModel sub)
    {
        var panel = new StackPanel { Spacing = 12, Width = 440, HorizontalAlignment = HorizontalAlignment.Center };

        var topCard = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorAltBrush"],
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 12, 14, 12)
        };
        var topSp = new StackPanel { Spacing = 8 };

        var titleRow = new Grid();
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleBlock = new TextBlock
        {
            Text = sub.Title,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(titleBlock, 0);
        titleRow.Children.Add(titleBlock);

        var statusColor = sub.Status switch
        {
            ItemStatus.Done => Windows.UI.Color.FromArgb(255, 46, 160, 67),
            ItemStatus.InProgress => Windows.UI.Color.FromArgb(255, 255, 170, 0),
            _ => Windows.UI.Color.FromArgb(255, 130, 130, 130)
        };
        var statusBadge = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor) { Opacity = 0.15 },
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10, 2, 10, 2),
            VerticalAlignment = VerticalAlignment.Center
        };
        statusBadge.Child = new TextBlock
        {
            Text = _localization.GetStatusText(sub.Status),
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor)
        };
        Grid.SetColumn(statusBadge, 1);
        titleRow.Children.Add(statusBadge);

        topSp.Children.Add(titleRow);

        if (!string.IsNullOrWhiteSpace(sub.Description))
        {
            topSp.Children.Add(new TextBlock
            {
                Text = sub.Description,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
        }
        topCard.Child = topSp;
        panel.Children.Add(topCard);

        if (!string.IsNullOrWhiteSpace(sub.ReviewNotes))
        {
            var notesCard = new Border
            {
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10)
            };
            var notesSp = new StackPanel { Spacing = 4 };
            notesSp.Children.Add(new TextBlock { Text = _localization.GetString("ReviewNotes"), FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            notesSp.Children.Add(new TextBlock { Text = sub.ReviewNotes, FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });
            notesCard.Child = notesSp;
            panel.Children.Add(notesCard);
        }

        var footer = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorAltBrush"],
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8, 12, 8)
        };
        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var createdFormatted = _localization.FormatDate(sub.CreatedDate);
        var createdBlock = new TextBlock { Text = $"{_localization.GetString("CreatedDateLabel")} {createdFormatted}", FontSize = 11, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] };
        Grid.SetColumn(createdBlock, 0);
        footerGrid.Children.Add(createdBlock);

        if (sub.ModifiedDate.HasValue)
        {
            var modifiedFormatted = _localization.FormatDate(sub.ModifiedDate.Value);
            var modBlock = new TextBlock { Text = $"{_localization.GetString("ModifiedDateLabel")} {modifiedFormatted}", FontSize = 11, HorizontalAlignment = HorizontalAlignment.Right, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] };
            Grid.SetColumn(modBlock, 1);
            footerGrid.Children.Add(modBlock);
        }
        footer.Child = footerGrid;
        panel.Children.Add(footer);

        var dialog = CreateStyledDialog(
            _localization.GetString("SubTaskSummaryDialogTitle"),
            panel,
            null,
            _localization.GetString("GotIt"),
            ContentDialogButton.Close);

        await dialog.ShowAsync();
    }
    #endregion

    #region Transfer Task (Copy / Move)
    private async void TransferTaskRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: HeadTaskViewModel task })
        {
            var availableProjects = ViewModel.Repositories
                .SelectMany(r => r.Projects)
                .Where(p => p.Id != task.ProjectId)
                .ToList();

            if (availableProjects.Count == 0)
            {
                var content = CreateConfirmationContent(_localization.GetString("NoOtherProjectMessage"), "\uE946", Windows.UI.Color.FromArgb(255, 0, 120, 212));

                var tip = CreateStyledDialog(
                    _localization.GetString("NoOtherProjectTitle"),
                    content,
                    null,
                    _localization.GetString("GotIt"),
                    ContentDialogButton.Close);

                await tip.ShowAsync();
                return;
            }

            var dialog = new TaskTransferDialog(task, availableProjects, GetWindow());
            if (await dialog.ShowAsync() == ContentDialogResult.Primary && dialog.SelectedProject != null)
            {
                if (dialog.IsCopy)
                {
                    await ViewModel.CopyHeadTaskAsync((task, dialog.SelectedProject.Id));
                }
                else
                {
                    await ViewModel.MoveHeadTaskAsync((task, dialog.SelectedProject.Id));
                }
            }
        }
    }
    #endregion

    #region SubTask Actions
    private async void ViewSubTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SubTaskViewModel sub })
        {
            await ShowSubTaskSummaryAsync(sub);
        }
    }

    private async void EditSubTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SubTaskViewModel sub })
        {
            await OpenSubTaskDetailAsync(sub);
        }
    }

    private async void DeleteSubTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SubTaskViewModel sub } && ViewModel.SelectedProject != null)
        {
            var message = string.Format(_localization.GetString("DeleteSubTaskConfirmMessageFormat"), sub.Title);
            var content = CreateConfirmationContent(message, "\uE7BA", Windows.UI.Color.FromArgb(255, 220, 53, 69));

            var confirm = CreateStyledDialog(
                _localization.GetString("DeleteSubTaskConfirmTitle"),
                content,
                _localization.GetString("YesDelete"),
                _localization.GetString("Cancel"),
                ContentDialogButton.Close);

            if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            {
                var parent = ViewModel.SelectedProject.HeadTasks.FirstOrDefault(t => t.Id == sub.HeadTaskId);
                if (parent != null)
                {
                    await ViewModel.DeleteSubTaskAsync((sub, parent));
                }
            }
        }
    }
    #endregion
}
