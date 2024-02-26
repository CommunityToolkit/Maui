using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Extension methods for <see cref="Popup"/>.
/// </summary>
public static partial class PopupExtensions
{
	static void PlatformShowPopup(Popup popup, IMauiContext mauiContext)
	{
		var window = mauiContext.GetPlatformWindow().GetWindow() ?? throw new NullReferenceException("Window is null.");

		Page parent = ((Page)window.Content).GetCurrentPage();
		parent?.AddLogicalChild(popup);

		var platform = popup.ToHandler(mauiContext);
		platform?.Invoke(nameof(IPopup.OnOpened));
	}

	static Task<object?> PlatformShowPopupAsync(Popup popup, IMauiContext mauiContext, CancellationToken token)
	{
		PlatformShowPopup(popup, mauiContext);
		return popup.Result.WaitAsync(token);
	}
}