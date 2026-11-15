using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DotDone.Services;
using DotDone.UI.ViewModels;

namespace DotDone.UI.Dialogs;

public sealed partial class TaskTransferDialog : ContentDialog
{
    public bool IsCopy => CopyRadio.IsChecked == true;
    public ProjectViewModel? SelectedProject => TargetProjectComboBox.SelectedItem as ProjectViewModel;

    public TaskTransferDialog(HeadTaskViewModel task, IEnumerable<ProjectViewModel> availableProjects, Window parentWindow)
    {
        InitializeComponent();
        XamlRoot = parentWindow.Content.XamlRoot;
        FontFamily = (Microsoft.UI.Xaml.Media.FontFamily)Application.Current.Resources["AppFontFamily"];
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        if (parentWindow.Content is FrameworkElement fe)
        {
            RequestedTheme = fe.RequestedTheme;
        }

        var loc = App.Services.GetRequiredService<ILocalizationService>();
        FlowDirection = loc.IsPersian ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Title = loc.GetString("TransferTitle");
        PrimaryButtonText = loc.GetString("Apply");
        CloseButtonText = loc.GetString("Cancel");

        TaskTitleBlock.Text = $"{loc.GetString("TransferTaskPrefix")} {task.Title}";
        OperationsHeaderBlock.Text = loc.GetString("TransferOperation");
        CopyRadio.Content = loc.GetString("TransferCopy");
        MoveRadio.Content = loc.GetString("TransferMove");
        TargetProjectLabel.Text = loc.GetString("TargetProject");

        TargetProjectComboBox.ItemsSource = availableProjects;
        TargetProjectComboBox.SelectedIndex = 0;
    }
}
