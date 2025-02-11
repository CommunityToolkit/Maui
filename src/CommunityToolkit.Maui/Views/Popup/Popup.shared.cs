using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
public partial class Popup : ContentView
{
	PopupContainer? popupContainer;

	/// <summary>
	/// Event occurs when the Popup is opened.
	/// </summary>
	public event EventHandler? OnOpened;

	/// <summary>
	/// Event occurs when the Popup is closed.
	/// </summary>
	public event EventHandler? OnClosed;

	/// <summary>
	/// Close the Popup.
	/// </summary>
	public async Task Close()
	{
		if (popupContainer is null)
		{
			return;
		}

		await popupContainer.Close(new PopupResult(false));
	}

	internal void SetPopup(PopupContainer container)
	{
		popupContainer = container;
	}

	internal void NotifyPopupIsOpened()
	{
		OnOpened?.Invoke(this, EventArgs.Empty);
	}

	internal void NotifyPopupIsClosed()
	{
		OnClosed?.Invoke(this, EventArgs.Empty);
	}
}

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
public partial class Popup<T> : Popup
{
	PopupContainer<T>? popupContainer;

	/// <summary>
	/// Close the Popup with a result.
	/// </summary>
	/// <param name="result">Popup result</param>
	public async Task Close(T result)
	{
		if (popupContainer is null)
		{
			return;
		}

		await popupContainer.Close(new PopupResult<T>(result, false));
	}

	internal void SetPopup(PopupContainer<T> container)
	{
		popupContainer = container;
	}
}