using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui;

/// <summary>
/// List-based implementation that manages the presentation of popups which will return the last item as the current popup.
/// </summary>
class PopupLifecycleController
{
	readonly List<WeakReference<PopupContainer>> currentPopups = [];

	public PopupContainer? GetCurrentPopup()
	{
		var popupReference = currentPopups.LastOrDefault();

		return popupReference?.TryGetTarget(out var popup) is true ? popup : null;
	}

	public void RegisterPopup(PopupContainer popup)
	{
		currentPopups.Add(new(popup));
	}

	public void UnregisterPopup(PopupContainer popup)
	{
		var matchingPopupReference = currentPopups.Find(reference => reference.TryGetTarget(out var target) && target == popup);

		if (matchingPopupReference is not null)
		{
			currentPopups.Remove(matchingPopupReference);
		}
	}
}