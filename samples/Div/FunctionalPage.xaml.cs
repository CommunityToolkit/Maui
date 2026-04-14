using System.Reflection;

namespace DivStart;

public partial class FunctionalPage : ContentPage
{
    public FunctionalPage()
    {
        InitializeComponent();
        //Loaded += (object? sender, EventArgs e) => Application.SetStatusBar();
        ColorButton.Text = "Change Color to " + NextColor.GetName();
        ColorLabel.Text = "Initial status bar color is blue";
    }

    List<Color> colorList = [Colors.Green, Colors.MediumAquamarine, Colors.Transparent];
    int colorListIndex = 0;
    Color NextColor => colorList[colorListIndex];
	void OnChangeColor(object? sender, EventArgs e)
    {
        Application.SetStatusBar(NextColor);
        ColorLabel.Text = "Current status bar color is " + NextColor.GetName();
        colorListIndex = (colorListIndex + 1) % colorList.Count;
        ColorButton.Text = "Change Color to " + NextColor.GetName();
    }
}
public static class ColorExtensions
{
    static readonly Dictionary<string, Color> namedColors;

    static ColorExtensions()
    {
        namedColors = typeof(Colors)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(Color))
            .ToDictionary(
                f => f.Name,
                f => (Color)f.GetValue(null)!);
    }

    public static string? GetName(this Color color)
    {
        foreach (var kvp in namedColors)
        {
            if (color == kvp.Value)
            {
	            return kvp.Key;
            }
        }

        return null;
    }
}