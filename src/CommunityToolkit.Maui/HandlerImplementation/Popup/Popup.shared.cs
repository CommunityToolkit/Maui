using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

public partial class Popup
{
	/// <summary>
	/// 
	/// </summary>
	public static CommandMapper<IPopup, PopupViewHandler> ControlPopUpCommandMapper = new(PopupViewHandler.PopUpCommandMapper)
	{
#if IOS || MACCATALYST
		[nameof(IPopup.OnOpened)] = MapOnOpened
#endif
	};

	internal static void RemapForControls()
	{
		PopupViewHandler.PopUpCommandMapper = ControlPopUpCommandMapper;
	}
}
