using System.Reflection;
using System.Windows;
using UsbMonitor.Helpers;
using UsbMonitor.ViewModels;

namespace UsbMonitor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        _comHelper = new ComHelper(this);

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
        {
            Title = $"{Title} v{version.Major}.{version.Minor}.{version.Build}";
        }

        Loaded += MainWindow_Loaded;
        Unloaded += MainWindow_Unloaded;
    }

    private readonly ComHelper _comHelper;

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _comHelper.RegisterForDeviceNotifications();
    }

    private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
    {
        _comHelper.UnregisterDeviceNotifications();

        if (DataContext is MainViewModel vm) vm.Dispose();
    }
}