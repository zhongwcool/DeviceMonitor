using CommunityToolkit.Mvvm.ComponentModel;

namespace UsbMonitor.Models;

public class Comport : ObservableObject
{
    private string _port = string.Empty;

    public string Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    private string _displayName = string.Empty;

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }
}