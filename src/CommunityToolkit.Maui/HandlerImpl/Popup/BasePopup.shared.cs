using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Core.Handlers;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;
public abstract partial class BasePopup
{
	/// <summary>
	/// 
	/// </summary>
	public static CommandMapper<IPopup, PopupViewHandler> ControlPopUpCommandMapper = new(PopupViewHandler.PopUpCommandMapper)
	{
#if IOS
		[nameof(IPopup.OnOpened)] = MapOnOpened
#endif
	};

	internal static void RemapForControls()
	{
		PopupViewHandler.PopUpCommandMapper = ControlPopUpCommandMapper;
	}
}
