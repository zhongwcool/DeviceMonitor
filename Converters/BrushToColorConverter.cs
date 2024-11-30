using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace UsbMonitor.Converters;

public class BrushToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SolidColorBrush brush)
            return Colors.Transparent;

        if (parameter == null || !double.TryParse(parameter.ToString(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var opacity)) return brush.Color;
        var color = brush.Color;
        var newAlpha = (byte)(color.A * opacity); // Calculate new alpha value

        // Return new color with adjusted alpha
        return Color.FromArgb(newAlpha, color.R, color.G, color.B);

        // If opacity parameter is not valid, return the original color
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}