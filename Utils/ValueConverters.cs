using System.Globalization;
using MauiAppIT13.Models;

namespace MauiAppIT13.Utils;

public class RoleColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Role role)
        {
            return role switch
            {
                Role.Student => Color.FromArgb("#DBEAFE"),
                Role.Teacher => Color.FromArgb("#DCFCE7"),
                Role.Admin => Color.FromArgb("#DBEAFE"),
                _ => Color.FromArgb("#F3F4F6")
            };
        }
        return Color.FromArgb("#F3F4F6");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class RoleTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Role role)
        {
            return role switch
            {
                Role.Student => Color.FromArgb("#1E40AF"),
                Role.Teacher => Color.FromArgb("#15803D"),
                Role.Admin => Color.FromArgb("#1E40AF"),
                _ => Color.FromArgb("#374151")
            };
        }
        return Color.FromArgb("#374151");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive ? Color.FromArgb("#D1FAE5") : Color.FromArgb("#FEE2E2");
        }
        return Color.FromArgb("#F3F4F6");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive ? "Active" : "Inactive";
        }
        return "Unknown";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive ? Color.FromArgb("#065F46") : Color.FromArgb("#991B1B");
        }
        return Color.FromArgb("#374151");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
