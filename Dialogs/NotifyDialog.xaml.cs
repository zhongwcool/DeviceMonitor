using System.Diagnostics;
using System.Windows;
using MaterialDesignThemes.Wpf;

namespace UsbMonitor.Dialogs;

public partial class NotifyDialog
{
    public NotifyDialog()
    {
        InitializeComponent();
    }

    public static async Task Show(string message, string dialogIdentifier)
    {
        var dialog = new NotifyDialog
        {
            Message = message
        };

        // show the dialog
        var result = await DialogHost.Show(dialog, dialogIdentifier);

        // check the result...
        Debug.WriteLine($"Dialog was closed: {result}", result ?? "NULL");
    }

    #region 属性

    private static readonly DependencyProperty TxtPositiveButtonProperty =
        DependencyProperty.Register(nameof(TxtPositiveButton), typeof(string), typeof(NotifyDialog),
            new PropertyMetadata("ACCEPT"));

    public string TxtPositiveButton
    {
        get => (string)GetValue(TxtPositiveButtonProperty);
        set => SetValue(TxtPositiveButtonProperty, value);
    }

    private static readonly DependencyProperty TxtNegativeButtonProperty =
        DependencyProperty.Register(nameof(TxtNegativeButton), typeof(string), typeof(NotifyDialog),
            new PropertyMetadata("CANCEL"));

    public string TxtNegativeButton
    {
        get => (string)GetValue(TxtNegativeButtonProperty);
        set => SetValue(TxtNegativeButtonProperty, value);
    }

    private static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(NotifyDialog),
            new PropertyMetadata(string.Empty));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    #endregion
}