using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;

namespace CommunityToolkit.Maui.Views;

public partial class Popup
{
	/// <summary>
	/// Popup CommandMapper
	/// </summary>
	public static CommandMapper<IPopup, PopupHandler> ControlPopUpCommandMapper = new(PopupHandler.PopUpCommandMapper)
	{
#if IOS || MACCATALYST
		[nameof(IPopup.OnOpened)] = MapOnOpened
#endif
	};

	internal static new void RemapForControls()
	{
		PopupHandler.PopUpCommandMapper = ControlPopUpCommandMapper;
	}
}