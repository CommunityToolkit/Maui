using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;
using UWPThickness = Microsoft.UI.Xaml.Thickness;
using XamlStyle = Microsoft.UI.Xaml.Style;

namespace CommunityToolkit.Core.Views;

/// <summary>
/// Extension class where Helper methods for Popup lives.
/// </summary>
public static class PopupExtensions
{
	/// <summary>
	/// Method to update the <see cref="Maui.Core.IPopup.Content"/> based on the <see cref="Maui.Core.IPopup.Color"/>.
	/// </summary>
	/// <param name="flyout">An instance of <see cref="MauiPopup"/>.</param>
	/// <param name="basePopup">An instance of <see cref="Maui.Core.IPopup"/>.</param>
	public static void SetColor(this MauiPopup flyout, IPopup basePopup)
	{
		ArgumentNullException.ThrowIfNull(basePopup.Content);

		var color = basePopup.Color ?? Colors.Transparent;
		var view = (View)basePopup.Content;
		if (view.BackgroundColor is null && flyout.Control is not null)
		{
			flyout.Control.Background = color.ToNative();
		}
	}
}
