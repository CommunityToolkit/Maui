namespace CommunityToolkit.Maui.Converters;
public class HexToColorConverter : IValueConverter {

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is string hexColor && hexColor.Length >= 6) // Checking for a minimum valid length
        {
            try {
                if (hexColor[0] == '#') {
                    hexColor = hexColor[1..]; // Remove '#' if present
                }

                Color color = Color.FromArgb(hexColor);
                return color;
            } catch (Exception e) {
                // Log the error to the output window
                Debug.WriteLine($"Error in converting hex color {value} \n Reason: {e.Message} ");
            }
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}