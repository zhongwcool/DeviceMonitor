using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using UsbMonitor.Dialogs;

namespace UsbMonitor.ViewModels;

public class MainViewModel : ObservableObject, IDisposable
{
    public MainViewModel()
    {
        UpdateSerialPortList();
        CommandOpen = new RelayCommand(OpenPort);
    }

    private SerialPort MyPort { get; set; }

    public ObservableCollection<string> SerialPortList { get; } = [];

    private string _selectedPort = string.Empty;

    public string SelectedPort
    {
        get => _selectedPort;
        set => SetProperty(ref _selectedPort, value);
    }

    public ICommand CommandOpen { get; set; }

    private void OpenPort()
    {
        if (MyPort is { IsOpen: true })
        {
            MyPort.Close();
            Print($"串口{MyPort.PortName}已关闭");
            BtnContent = "Connect";
        }
        else
        {
            if (SelectedPort != null)
            {
                MyPort = new SerialPort(SelectedPort, 115200);
                try
                {
                    MyPort.Open();
                    Print($"串口{MyPort.PortName}已打开");
                    BtnContent = "Disconnect";
                }
                catch (Exception ex)
                {
                    Print($"打开串口时出错: {ex.Message}");
                }
            }
            else
            {
                var dialog = new NotifyDialog
                {
                    Message = "请选择一个串口"
                };
                _ = DialogHost.Show(dialog, "Dialog_Root_Main");
            }
        }
    }

    private string _btnContent = "Connect";

    public string BtnContent
    {
        get => _btnContent;
        set => SetProperty(ref _btnContent, value);
    }

    public ObservableCollection<string> HistoryList { get; } = [];

    public void HandleDeviceRemoval()
    {
        // 检查当前打开的串口是否仍然存在
        if (MyPort == null || SerialPort.GetPortNames().Contains(MyPort.PortName)) return;

        // 当前打开的串口已移除，关闭串口并提示用户
        if (MyPort.IsOpen)
        {
            MyPort.Close();
        }

        Print("USB device and serial port removed");
        BtnContent = "Connect";

        // 移除此设备的串口
        SerialPortList.Remove(MyPort.PortName);
        MyPort = null;

        var dialog = new NotifyDialog
        {
            Message = "串口设备被拔除"
        };
        _ = DialogHost.Show(dialog, "Dialog_Root_Main");
    }

    public void UpdateSerialPortList()
    {
        var currentPorts = SerialPort.GetPortNames();
        SerialPortList.Clear();
        foreach (var port in currentPorts)
        {
            SerialPortList.Add(port);
        }

        Print($"找到了{SerialPortList.Count}个串口设备");

        if (currentPorts.Contains(MyPort?.PortName))
        {
            // 当前连接的串口已被移除
            HandleDeviceRemoval();
        }

        SelectedPort = SerialPortList.FirstOrDefault();
    }

    public void Print(string body)
    {
        // 加上时间戳
        var message = $"{DateTime.Now:HH:mm:ss.fff} - {body}";
        HistoryList.Insert(0, message);
    }

    #region IDisposable

    private void ReleaseUnmanagedResources()
    {
        // TODO release unmanaged resources here
    }

    private void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
            MyPort?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~MainViewModel()
    {
        Dispose(false);
    }

    #endregion
}