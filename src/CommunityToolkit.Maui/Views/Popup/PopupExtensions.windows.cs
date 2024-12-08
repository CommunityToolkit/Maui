using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Extension methods for <see cref="Popup"/>.
/// </summary>
public static partial class PopupExtensions
{
	static void PlatformShowPopup(Popup popup, IMauiContext mauiContext)
	{
		if (mauiContext.GetPlatformWindow().GetWindow()?.Content is not Page parent)
		{
			throw new InvalidOperationException("Window Content cannot be null");
		}

		parent.AddLogicalChild(popup);

		var platform = popup.ToHandler(mauiContext);
		platform?.Invoke(nameof(IPopup.OnOpened));
	}

	static Task<object?> PlatformShowPopupAsync(Popup popup, IMauiContext mauiContext, CancellationToken token)
	{
		PlatformShowPopup(popup, mauiContext);
		return popup.Result.WaitAsync(token);
	}
}