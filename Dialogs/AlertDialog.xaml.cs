using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace UsbMonitor.Dialogs;

public partial class AlertDialog : UserControl
{
    public AlertDialog()
    {
        InitializeComponent();
    }

    public static async Task Show(string message, string txtPositive, string txtNegative,
        Func<CancellationToken, Task<(int, string)>> longRunningTaskDelegate, string dialogIdentifier)
    {
        var tokenSource = new CancellationTokenSource();
        var dialog = new AlertDialog
        {
            Message = message,
            TxtPositiveButton = txtPositive,
            TxtNegativeButton = txtNegative
        };

        // show the dialog
        var result = await DialogHost.Show(dialog, dialogIdentifier);

        // check the result...
        Debug.WriteLine($"Dialog was closed: {result}");
        if (result is not bool needRedirect) return;
        if (needRedirect)
        {
            var ret = await longRunningTaskDelegate(tokenSource.Token); // 调用耗时的任务
            var dialog0 = new NotifyDialog
            {
                Message = ret.Item2
            };
            await DialogHost.Show(dialog0, dialogIdentifier);
        }
    }

    #region 属性

    private static readonly DependencyProperty TxtPositiveButtonProperty =
        DependencyProperty.Register(nameof(TxtPositiveButton), typeof(string), typeof(AlertDialog),
            new PropertyMetadata("ACCEPT"));

    public string TxtPositiveButton
    {
        get => (string)GetValue(TxtPositiveButtonProperty);
        set => SetValue(TxtPositiveButtonProperty, value);
    }

    private static readonly DependencyProperty TxtNegativeButtonProperty =
        DependencyProperty.Register(nameof(TxtNegativeButton), typeof(string), typeof(AlertDialog),
            new PropertyMetadata("CANCEL"));

    public string TxtNegativeButton
    {
        get => (string)GetValue(TxtNegativeButtonProperty);
        set => SetValue(TxtNegativeButtonProperty, value);
    }

    private static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(AlertDialog),
            new PropertyMetadata(string.Empty));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    private static readonly DependencyProperty CountdownProperty =
        DependencyProperty.Register(nameof(Countdown), typeof(int), typeof(AlertDialog), new PropertyMetadata(0));

    public int Countdown
    {
        get => (int)GetValue(CountdownProperty);
        set => SetValue(CountdownProperty, value);
    }

    #endregion
}