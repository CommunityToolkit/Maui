#if WINDOWS
using CommunityToolkit.Core.Handlers;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.PlatformConfiguration.WindowsSpecific;
namespace CommunityToolkit.Maui.Views;

public partial class Popup
{
	public static PropertyMapper<IPopup, PopupViewHandler> ControlsPopupMapper = new(PopupViewHandler.PopUpMapper)
	{
		["BorderColor"] = MapBorderColor
	};

	internal static void RemapForControls()
	{
		PopupViewHandler.PopUpMapper = ControlsPopupMapper;
	}

	public static void MapBorderColor(PopupViewHandler handler, IPopup view)
	{
		var borderColor = PopUpConfiguration.GetBorderColor((BindableObject)view);
		handler.NativeView.SetBorderColor(borderColor);
	}

}

#endif