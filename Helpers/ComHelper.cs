using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace UsbMonitor.Helpers;

public class ComHelper(Window window)
{
    private const int WM_DEVICECHANGE = 0x0219;
    private const int DBT_DEVICEARRIVAL = 0x8000;
    private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
    private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
    private const int DBT_DEVTYP_DEVICEINTERFACE = 0x0005;

    private static readonly Guid GUID_DEVINTERFACE_USB_DEVICE = new("A5DCBF10-6530-11D2-901F-00C04FB951ED");

    private IntPtr _notificationHandle;

    public void RegisterForDeviceNotifications()
    {
        var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
        if (hwndSource == null) throw new Exception("Unable to monitor device changes.");
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

        _notificationHandle = RegisterDeviceNotification(hwndSource.Handle, buffer, DEVICE_NOTIFY_WINDOW_HANDLE);
    }

    public void UnregisterDeviceNotifications()
    {
        if (_notificationHandle != IntPtr.Zero)
        {
            UnregisterDeviceNotification(_notificationHandle);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_DEVICECHANGE) return IntPtr.Zero;
        var eventCode = wParam.ToInt32();
        string eventMessage;

        switch (eventCode)
        {
            case DBT_DEVICEARRIVAL:
                eventMessage = "USB device inserted";
                DeviceArrived?.Invoke();
                break;
            case DBT_DEVICEREMOVECOMPLETE:
                eventMessage = "USB device removed";
                MoveCompleted?.Invoke();
                break;
            default:
                eventMessage = "Other device change event";
                break;
        }

        // 更新UI
        LogPrint?.Invoke(eventMessage);

        return IntPtr.Zero;
    }

    public event DeviceArrivalHandler DeviceArrived;

    public delegate void DeviceArrivalHandler();

    public event DeviceMoveCompleteHandler MoveCompleted;

    public delegate void DeviceMoveCompleteHandler();

    public event LogPrintHandler LogPrint;

    public delegate void LogPrintHandler(string message);

    #region P/Invoke

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