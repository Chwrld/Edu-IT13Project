using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiAppIT13.Utils;

public class NetworkMonitor : INotifyPropertyChanged
{
    private static NetworkMonitor? _instance;
    private bool _isConnected;

    public static NetworkMonitor Instance => _instance ??= new NetworkMonitor();

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (_isConnected != value)
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }
    }

    private NetworkMonitor()
    {
        _isConnected = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        IsConnected = e.NetworkAccess == NetworkAccess.Internet;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
