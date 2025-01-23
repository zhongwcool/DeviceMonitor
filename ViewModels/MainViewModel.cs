using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UsbMonitor.Dialogs;
using UsbMonitor.Models;

namespace UsbMonitor.ViewModels;

public class MainViewModel : ObservableObject, IDisposable
{
    public MainViewModel()
    {
        _ = UpdateSerialPortList();
        CommandOpen = new AsyncRelayCommand(OpenPort);
    }

    private SerialPort MyPort { get; set; }

    public ObservableCollection<Comport> SerialPortList { get; } = [];

    private Comport _selectedPort;

    public Comport SelectedPort
    {
        get => _selectedPort;
        set => SetProperty(ref _selectedPort, value);
    }

    public IAsyncRelayCommand CommandOpen { get; set; }

    private async Task OpenPort()
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
                MyPort = new SerialPort(SelectedPort.Port, 115200);
                try
                {
                    MyPort.Open();
                    Print($"串口{MyPort.PortName}已打开");
                    BtnContent = "Disconnect";
                }
                catch (UnauthorizedAccessException)
                {
                    Print($"打开串口{MyPort.PortName}时出错: 串口被占用或者没有足够权限");
                    _ = NotifyDialog.Show("串口被占用或者没有足够权限", "Dialog_Root_Main");
                }
                catch (IOException)
                {
                    Print($"打开串口{MyPort.PortName}时出错: 串口已被占用");
                    _ = NotifyDialog.Show("串口已被占用", "Dialog_Root_Main");
                }
                catch (ArgumentException)
                {
                    Print($"打开串口{MyPort.PortName}时出错: 指定的串口不是有效的");
                    _ = NotifyDialog.Show("指定的串口不是有效的", "Dialog_Root_Main");
                }
            }
            else
            {
                _ = NotifyDialog.Show("请选择一个串口", "Dialog_Root_Main");
            }
        }

        await Task.CompletedTask;
    }

    private string _btnContent = "Connect";

    public string BtnContent
    {
        get => _btnContent;
        set => SetProperty(ref _btnContent, value);
    }

    public ObservableCollection<string> HistoryList { get; } = [];

    private async Task HandleDeviceRemoval()
    {
        // 检查当前打开的串口是否仍然存在
        if (MyPort == null || SerialPort.GetPortNames().Contains(MyPort.PortName)) return;

        // 当前打开的串口已移除，关闭串口并提示用户
        if (MyPort.IsOpen) MyPort.Close();

        Print("USB device and serial port removed");
        BtnContent = "Connect";

        // 移除此设备的串口
        var comPortToRemove = SerialPortList.FirstOrDefault(comport => comport.Port == MyPort.PortName);
        if (comPortToRemove != null) SerialPortList.Remove(comPortToRemove);

        // 提示用户
        _ = NotifyDialog.Show($"串口设备({MyPort.PortName})被拔除", "Dialog_Root_Main");
        MyPort = null;

        await Task.CompletedTask;
    }

    private bool _inUpdate;

    public async Task UpdateSerialPortList()
    {
        if (_inUpdate) return;
        _inUpdate = true;
        try
        {
            var foundPorts = SerialPort.GetPortNames();
            // 移除不存在的串口
            var portsToRemove = SerialPortList.Where(port => !foundPorts.Contains(port.Port)).ToList();
            foreach (var port in portsToRemove)
            {
                SerialPortList.Remove(port);
            }

            foreach (var port in foundPorts)
            {
                // 如果串口已经存在，跳过
                if (SerialPortList.Any(p => p.Port == port)) continue;
                SerialPortList.Add(new Comport { Port = port, DisplayName = port });
            }

            var ret = TryToRichPortsViaWmi();
            if (ret != 0) _ = Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(_ => { TryToRichPortsViaWmi(); });

            Print($"找到了{SerialPortList.Count}个串口设备");

            if (MyPort != null && !foundPorts.Contains(MyPort?.PortName))
            {
                // 当前连接的串口已被移除
                _ = HandleDeviceRemoval();
            }

            SelectedPort = SerialPortList.FirstOrDefault();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _inUpdate = false;
        }

        await Task.CompletedTask;
    }

    private int TryToRichPortsViaWmi()
    {
        try
        {
            // 初始化 WMI 查询用来获取串口设备信息
            var searcher = new ManagementObjectSearcher("root\\CIMV2",
                "SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%'");

            // 执行查询并输出每一个匹配对象的描述
            foreach (var obj in searcher.Get())
            {
                var raw = (string)obj["Caption"];
                TrimComport(raw);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Print("Access denied. Ensure your application has the right permissions.");
            Console.WriteLine(ex.Message);
            return -1;
        }
        catch (ManagementException ex)
        {
            Print("WMI query failed.");
            Console.WriteLine(ex.Message);
            return -2;
        }
        catch (InvalidCastException ex)
        {
            Print("Specified cast is not valid.");
            Console.WriteLine(ex.Message);
            return -3;
        }
        catch (Exception ex)
        {
            Print("An unexpected error occurred.");
            Console.WriteLine(ex.Message);
            return -4;
        }

        return 0;

        void TrimComport(string input)
        {
            // 使用正则表达式匹配括号之前的所有字符（不包括括号本身）
            // like "Electronic Team Virtual Serial Port (COM10->COM11)"
            var match1 = Regex.Match(input, @"^(.+?)\s*\(");
            var match2 = Regex.Match(input, @"\(([^)]*)\)");

            if (!match1.Success || !match2.Success) return;
            // 返回括号前的字符串
            var describe = match1.Groups[1].Value.Trim();
            var pairedPort = match2.Groups[1].Value.Trim();
            var ports = pairedPort.Split(["->"], StringSplitOptions.None);
            var port = SerialPortList.FirstOrDefault(p => p.Port == ports[0]);
            if (port != null) port.DisplayName = ports[0] + " " + describe;
        }
    }

    public void Print(string body)
    {
        // 加上时间戳
        var message = $"{DateTime.Now:HH:mm:ss.fff} - {body}";
        // 更新UI
        Dispatcher.CurrentDispatcher.Invoke(() => HistoryList.Insert(0, message));
    }

    #region IDisposable

    private void ReleaseUnmanagedResources()
    {
        //TODO release unmanaged resources here
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