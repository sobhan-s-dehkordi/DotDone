using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DotDone.UI.ViewModels;

namespace DotDone.UI.Dialogs;

public sealed partial class StreakGapDialog : ContentDialog
{
    public StreakGapDialogViewModel ViewModel { get; }

    public StreakGapDialog(StreakGapDialogViewModel viewModel, Window parentWindow)
    {
        InitializeComponent();
        ViewModel = viewModel;
        FlowDirection = viewModel.FlowDirection;
        XamlRoot = parentWindow.Content.XamlRoot;
        FontFamily = (Microsoft.UI.Xaml.Media.FontFamily)Application.Current.Resources["AppFontFamily"];
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        if (parentWindow.Content is FrameworkElement fe)
        {
            RequestedTheme = fe.RequestedTheme;
        }

        PrimaryButtonClick += async (s, e) =>
        {
            await ViewModel.ConfirmRecoveryAsync();
        };

        CloseButtonClick += async (s, e) =>
        {
            await ViewModel.ChooseNoResetAsync();
        };
    }
}
