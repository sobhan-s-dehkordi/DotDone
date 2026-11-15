using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using DotDone.Core.Enums;

namespace DotDone.UI.Converters;

public class StatusToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush TodoBrush = new(Windows.UI.Color.FromArgb(255, 140, 140, 140));
    private static readonly SolidColorBrush InProgressBrush = new(Windows.UI.Color.FromArgb(255, 255, 170, 0));
    private static readonly SolidColorBrush DoneBrush = new(Windows.UI.Color.FromArgb(255, 46, 160, 67));

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ItemStatus status)
        {
            return status switch
            {
                ItemStatus.Todo => TodoBrush,
                ItemStatus.InProgress => InProgressBrush,
                ItemStatus.Done => DoneBrush,
                _ => TodoBrush
            };
        }
        return TodoBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class StatusToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ItemStatus status)
        {
            return status switch
            {
                ItemStatus.Todo => "\uEA3A",        // RadioButtonUnchecked
                ItemStatus.InProgress => "\uE768",  // ProgressRing / Play
                ItemStatus.Done => "\uE73E",        // CheckMark
                _ => "\uEA3A"
            };
        }
        return "\uEA3A";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class StatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ItemStatus status)
        {
            return status switch
            {
                ItemStatus.Todo => "انجام نشده",
                ItemStatus.InProgress => "درحال انجام",
                ItemStatus.Done => "انجام شده",
                _ => "انجام نشده"
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class FlowDirectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is AppLanguage lang && lang == AppLanguage.Persian)
        {
            return FlowDirection.RightToLeft;
        }
        return FlowDirection.LeftToRight;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class BoolToChevronConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b && b)
        {
            return "\uE70E"; // ChevronUp
        }
        return "\uE70D"; // ChevronDown
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count && count > 0)
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool b = value is bool flag && flag;
        if (parameter is string param && param.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            b = !b;
        }
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool notNull = value != null;
        if (parameter is string param && param.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            notNull = !notNull;
        }
        return notNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
