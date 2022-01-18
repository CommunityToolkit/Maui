using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Core.Handlers;
public partial class PopupViewHandler
{
	public static PropertyMapper<IBasePopup, PopupViewHandler> PopUpMapper = new(ElementMapper)
	{
		[nameof(IBasePopup.Anchor)] = MapAnchor,
		[nameof(IBasePopup.Color)] = MapColor,
		[nameof(IBasePopup.Size)] = MapSize,
		[nameof(IBasePopup.VerticalOptions)] = MapSize,
		[nameof(IBasePopup.HorizontalOptions)] = MapSize,
		[nameof(IBasePopup.IsLightDismissEnabled)] = MapLightDismiss
	};

	public static CommandMapper<IBasePopup, PopupViewHandler> PopUpCommandMapper = new(ElementCommandMapper)
	{
		[nameof(IBasePopup.OnDismissed)] = MapOnDismissed,
		[nameof(IBasePopup.OnOpened)] = MapOnOpened,
		[nameof(IBasePopup.LightDismiss)] = MapOnLightDismiss
	};

	public PopupViewHandler(PropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? PopUpMapper, commandMapper ?? PopUpCommandMapper)
	{
	}


	public PopupViewHandler()
		: base(PopUpMapper, PopUpCommandMapper)
	{
	}
}

