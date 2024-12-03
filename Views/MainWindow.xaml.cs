using System.Reflection;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using UsbMonitor.Helpers;
using UsbMonitor.ViewModels;

namespace UsbMonitor.Views;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();

        // 加载主题
        UpdateTheme();
        // 监听系统主题变化
        SystemEvents.UserPreferenceChanged += (_, args) =>
        {
            // 当事件是由于主题变化引起的
            if (args.Category == UserPreferenceCategory.General)
            {
                // 这里你可以写代码来处理主题变化，例如，重新加载样式或者资源
                UpdateTheme();
            }
        };

        _comHelper = new ComHelper(this);
        _comHelper.DeviceArrived += () =>
        {
            if (DataContext is MainViewModel vm) _ = vm.UpdateSerialPortList();
        };
        _comHelper.MoveCompleted += () =>
        {
            if (DataContext is not MainViewModel vm) return;
            _ = vm.HandleDeviceRemoval();
            _ = vm.UpdateSerialPortList();
        };
        _comHelper.LogPrint += (msg) =>
        {
            if (DataContext is MainViewModel vm) vm.Print(msg);
        };

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

    #region Theme Change

    private static void UpdateTheme()
    {
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        // 检查当前主题并应用
        switch (Theme.GetSystemTheme())
        {
            case BaseTheme.Light:
                theme.SetBaseTheme(BaseTheme.Light);
                break;
            case BaseTheme.Dark:
                theme.SetBaseTheme(BaseTheme.Dark);
                break;
        }

        paletteHelper.SetTheme(theme);
    }

    #endregion
}