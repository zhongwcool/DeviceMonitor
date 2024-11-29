using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using UsbMonitor.ViewModels;

namespace UsbMonitor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
        {
            Title = $"{Title} v{version.Major}.{version.Minor}.{version.Build}";
        }

        Loaded += MainWindow_Loaded;
        Unloaded += MainWindow_Unloaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RegisterForDeviceNotifications();
    }

    private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
    {
        if (notificationHandle != IntPtr.Zero)
        {
            UnregisterDeviceNotification(notificationHandle);
        }

        if (DataContext is MainViewModel vm) vm.Dispose();
    }

    private void RegisterForDeviceNotifications()
    {
        var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        if (hwndSource == null) throw new Exception("无法监控设备更改");
        hwndSource.AddHook(WndProc);

        var dbi = new DEV_BROADCAST_DEVICEINTERFACE
        {
            dbcc_size = Marshal.SizeOf<DEV_BROADCAST_DEVICEINTERFACE>(),
            dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE,
            dbcc_reserved = 0,
            dbcc_classguid = GUID_DEVINTERFACE_USB_DEVICE
        };

        var buffer = Marshal.AllocHGlobal(dbi.dbcc_size);
        Marshal.StructureToPtr(dbi, buffer, true);

        notificationHandle = RegisterDeviceNotification(hwndSource.Handle, buffer, DEVICE_NOTIFY_WINDOW_HANDLE);
    }

    private const int WM_DEVICECHANGE = 0x0219;
    private const int DBT_DEVICEARRIVAL = 0x8000;
    private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
    private IntPtr notificationHandle;

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_DEVICECHANGE) return IntPtr.Zero;
        var eventCode = wParam.ToInt32();
        string eventMessage;

        switch (eventCode)
        {
            case DBT_DEVICEARRIVAL:
                eventMessage = "USB device inserted";
                if (DataContext is MainViewModel vm0) vm0.UpdateSerialPortList();
                break;
            case DBT_DEVICEREMOVECOMPLETE:
                eventMessage = "USB device removed";
                if (DataContext is MainViewModel vm1)
                {
                    vm1.HandleDeviceRemoval();
                    vm1.UpdateSerialPortList();
                }

                break;
            default:
                eventMessage = "Other device change event";
                break;
        }

        LogDeviceChangeEvent(eventCode, eventMessage);

        // 更新UI
        Dispatcher.Invoke(() =>
        {
            if (DataContext is MainViewModel vm2) vm2.Print(eventMessage);
        });

        return IntPtr.Zero;
    }

    private void LogDeviceChangeEvent(int eventCode, string message)
    {
        var logEntry = $"{DateTime.Now}: Event Code {eventCode} - {message}";
        Debug.WriteLine(logEntry);
        // 可选：将日志写入文件或其他日志存储
        // File.AppendAllText("DeviceChangeLog.txt", logEntry + Environment.NewLine);
    }

    #region P/Invoke

    private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
    private const int DBT_DEVTYP_DEVICEINTERFACE = 0x0005;

    private static readonly Guid GUID_DEVINTERFACE_USB_DEVICE = new("A5DCBF10-6530-11D2-901F-00C04FB951ED");

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter, int flags);

    [DllImport("user32.dll")]
    private static extern bool UnregisterDeviceNotification(IntPtr handle);

    [StructLayout(LayoutKind.Sequential)]
    private struct DEV_BROADCAST_DEVICEINTERFACE
    {
        public int dbcc_size;
        public int dbcc_devicetype;
        public int dbcc_reserved;
        public Guid dbcc_classguid;
        public short dbcc_name;
    }

    #endregion
}