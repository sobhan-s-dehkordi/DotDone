using System;
using System.Linq;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DotDone.Core.Enums;
using DotDone.UI.ViewModels;
using WinRT.Interop;

namespace DotDone.UI.Dialogs;

public sealed partial class TaskDetailDialog : ContentDialog
{
    public DetailDialogViewModel ViewModel { get; }
    private readonly Window _parentWindow;

    public TaskDetailDialog(DetailDialogViewModel viewModel, Window parentWindow)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _parentWindow = parentWindow;
        XamlRoot = parentWindow.Content.XamlRoot;
        FlowDirection = viewModel.FlowDirection;
        FontFamily = (Microsoft.UI.Xaml.Media.FontFamily)Application.Current.Resources["AppFontFamily"];
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        if (parentWindow.Content is FrameworkElement fe)
        {
            RequestedTheme = fe.RequestedTheme;
        }

        // Set initial status selection
        StatusComboBox.SelectedIndex = (int)ViewModel.Status;

        PrimaryButtonClick += (s, e) =>
        {
            if (StatusComboBox.SelectedIndex >= 0)
            {
                ViewModel.Status = (ItemStatus)StatusComboBox.SelectedIndex;
            }
        };
    }

    private async void AddAttachment_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.ViewMode = PickerViewMode.List;
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(_parentWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            await ViewModel.AddAttachmentAsync(file.Path);
        }
    }

    private void OpenAttachment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: AttachmentViewModel att })
        {
            ViewModel.OpenAttachment(att);
        }
    }

    private async void ConfirmDeleteAttachment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: AttachmentViewModel att })
        {
            await ViewModel.DeleteAttachmentAsync(att);
        }
    }
}
