using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
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
        UpdateIcon();
        // 监听系统主题变化
        SystemEvents.UserPreferenceChanged += (_, args) =>
        {
            // 当事件是由于主题变化引起的
            if (args.Category != UserPreferenceCategory.General) return;
            // 这里你可以写代码来处理主题变化，例如，重新加载样式或者资源
            UpdateTheme();
            UpdateIcon();
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

    // 更改托盘图标的函数
    private void UpdateIcon()
    {
        var isDark = UseDarkSystemTheme();
        if (isDark)
        {
            var iconUri = new Uri("pack://application:,,,/Resources/Dark/main.ico", UriKind.RelativeOrAbsolute);
            Icon = BitmapFrame.Create(iconUri);
        }
        else
        {
            var iconUri = new Uri("pack://application:,,,/Resources/Light/main.ico", UriKind.RelativeOrAbsolute);
            Icon = BitmapFrame.Create(iconUri);
        }
    }

    private static bool UseDarkSystemTheme()
    {
        // 在注册表中，Windows保存它的个人设置信息
        // 目前Windows将AppsUseLightTheme键值用于表示深色或浅色主题
        // 该键值位于路径HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize下
        const string registryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        const string registryValueName = "AppsUseLightTheme";

        using var key = Registry.CurrentUser.OpenSubKey(registryKeyPath);
        var registryValueObject = key?.GetValue(registryValueName)!;

        var registryValue = (int?)registryValueObject;

        // AppsUseLightTheme 0表示深色，1表示浅色
        return registryValue == 0;
    }

    #endregion
}