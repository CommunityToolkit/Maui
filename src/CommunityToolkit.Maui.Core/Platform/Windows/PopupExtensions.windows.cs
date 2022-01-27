using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
//using Specific = CommunityToolkit.Maui.PlatformConfiguration.WindowsSpecific.PopUp;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;
using UWPThickness = Microsoft.UI.Xaml.Thickness;
using CommunityToolkit.Maui.Core.Platform;
using XamlStyle = Microsoft.UI.Xaml.Style;

namespace CommunityToolkit.Core.Platform;

public static class PopupExtensions
{
	public static void SetColor(this PopupRenderer flyout, IBasePopup basePopup)
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
