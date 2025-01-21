using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a small View that pops up at front the Page. Implements <see cref="PopupContainer"/>.
/// </summary>
public partial class Popup : ContentView
{
	PopupContainer? popupContainer;

	/// <summary>
	/// 
	/// </summary>
	public event EventHandler? OnOpened;

	/// <summary>
	/// 
	/// </summary>
	public event EventHandler? OnClosed;

	/// <summary>
	/// 
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
/// Represents a small View that pops up at front the Page. Implements <see cref="PopupContainer"/>.
/// </summary>
public partial class Popup<T> : Popup
{
	PopupContainer<T>? popupContainer;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="result"></param>
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