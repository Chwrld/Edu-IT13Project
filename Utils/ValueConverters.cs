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

public class NetworkStatusBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? Color.FromArgb("#E8FBF4") : Color.FromArgb("#FDEBEC");
        }
        return Color.FromArgb("#FDEBEC");
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
        if (value is string status)
        {
            var normalized = User.NormalizeStatus(status);
            return normalized switch
            {
                User.StatusActive => Color.FromArgb("#D1FAE5"),
                User.StatusInactive => Color.FromArgb("#FEF3C7"),
                User.StatusArchived => Color.FromArgb("#FEE2E2"),
                _ => Color.FromArgb("#F3F4F6")
            };
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
        if (value is string status)
        {
            var normalized = User.NormalizeStatus(status);
            return normalized switch
            {
                User.StatusActive => "Active",
                User.StatusInactive => "Inactive",
                User.StatusArchived => "Archived",
                _ => "Unknown"
            };
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
        if (value is string status)
        {
            var normalized = User.NormalizeStatus(status);
            return normalized switch
            {
                User.StatusActive => Color.FromArgb("#065F46"),
                User.StatusInactive => Color.FromArgb("#92400E"),
                User.StatusArchived => Color.FromArgb("#991B1B"),
                _ => Color.FromArgb("#374151")
            };
        }
        return Color.FromArgb("#374151");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NetworkStatusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? "📶" : "📵";
        }
        return "📵";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NetworkStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444");
        }
        return Color.FromArgb("#EF4444");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NetworkStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? "Online" : "Offline";
        }
        return "Offline";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
