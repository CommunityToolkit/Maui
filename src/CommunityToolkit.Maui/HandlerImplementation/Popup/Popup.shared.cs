using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

public partial class Popup
{
	/// <summary>
	/// 
	/// </summary>
	public static CommandMapper<IPopup, PopupHandler> ControlPopUpCommandMapper = new(PopupHandler.PopUpCommandMapper)
	{
#if IOS || MACCATALYST
		[nameof(IPopup.OnOpened)] = MapOnOpened
#endif
	};

	internal static void RemapForControls()
	{
		PopupHandler.PopUpCommandMapper = ControlPopUpCommandMapper;
	}
}
