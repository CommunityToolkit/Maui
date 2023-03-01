using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Media;

namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extensions for <see cref="Brush"/>
/// </summary>
public static class BrushExtensions
{
	/// <summary>
	/// Convert <see cref="Brush"/> to <see cref="Color"/>
	/// </summary>
	public static Color? ToColor(this Brush brush)
	{
		if (brush is SolidColorBrush solidColorBrush)
		{
			return solidColorBrush.Color.ToColor();
		}

		return null;
	}
}