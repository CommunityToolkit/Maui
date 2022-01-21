#if WINDOWS
using CommunityToolkit.Core.Handlers;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.PlatformConfiguration.WindowsSpecific;
namespace CommunityToolkit.Maui.Views;

public partial class Popup
{
	public static PropertyMapper<IBasePopup, PopupViewHandler> ControlsPopupMapper = new(PopupViewHandler.PopUpMapper)
	{
		["BorderColor"] = MapBorderColor
	};

	internal static void RemapForControls()
	{
		PopupViewHandler.PopUpMapper = ControlsPopupMapper;
	}

	public static void MapBorderColor(PopupViewHandler handler, IBasePopup view)
	{
		var borderColor = PopUpConfiguration.GetBorderColor((BindableObject)view);
		handler.NativeView.SetBorderColor(borderColor);
	}

}

#endif