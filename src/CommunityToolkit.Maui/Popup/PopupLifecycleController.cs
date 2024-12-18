using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui;

/// <summary>
/// List-based implementation that manages the presentation of popups which will return the last item as the current popup.
/// </summary>
public class PopupLifecycleController : IPopupLifecycleController
{
	readonly List<WeakReference<Popup>> currentPopups = [];

	/// <inheritdoc cref="IPopupLifecycleController.GetCurrentPopup"/>
	public Popup? GetCurrentPopup()
	{
		var popupReference = currentPopups.LastOrDefault();

		return popupReference?.TryGetTarget(out var popup) is true ? popup : null;
	}

	/// <inheritdoc cref="IPopupLifecycleController.OnShowPopup"/>
	public void OnShowPopup(Popup popup)
	{
		popup.Closed += OnPopupClosed;
		currentPopups.Add(new(popup));
	}

	void OnPopupClosed(object? sender, PopupClosedEventArgs e)
	{
		if (sender is not Popup popup)
		{
			return;
		}

		popup.Closed -= OnPopupClosed;
		var matchingPopupReference = currentPopups.Find(reference => reference.TryGetTarget(out var target) && target == popup);

		if (matchingPopupReference is not null)
		{
			currentPopups.Remove(matchingPopupReference);
		}
	}
}