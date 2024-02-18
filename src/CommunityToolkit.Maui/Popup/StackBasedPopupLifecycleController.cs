using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui;

/// <summary>
/// <see cref="Stack{T}"/> based implementation that manages the presentation of popups.
/// </summary>
public class StackBasedPopupLifecycleController : IPopupLifecycleController
{
	readonly Stack<WeakReference<Popup>> popupDisplayStack = new();

	/// <inheritdoc cref="IPopupLifecycleController.GetCurrentPopup"/>
	public Popup? GetCurrentPopup()
	{
		var popupReference = popupDisplayStack.Peek();

		if (popupReference.TryGetTarget(out var popup))
		{
			return popup;
		}

		return null;
	}

	/// <inheritdoc cref="IPopupLifecycleController.OnShowPopup"/>
	public void OnShowPopup(Popup popup)
	{
		popup.Closed += OnPopupClosed;
		popupDisplayStack.Push(new(popup));
	}

	void OnPopupClosed(object? sender, PopupClosedEventArgs e)
	{
		if (sender is not Popup popup)
		{
			return;
		}

		popup.Closed -= OnPopupClosed;
		popupDisplayStack.Pop();
	}
}