using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
public partial class Popup : ContentView
{
	/// <summary>
	/// Event occurs when the Popup is opened.
	/// </summary>
	public event EventHandler? Opened;

	/// <summary>
	/// Event occurs when the Popup is closed.
	/// </summary>
	public event EventHandler? Closed;

	/// <summary>
	/// Close the Popup.
	/// </summary>
	public virtual Task Close() => GetPopupContainer().Close(new PopupResult(false));

	internal void NotifyPopupIsOpened()
	{
		Opened?.Invoke(this, EventArgs.Empty);
	}

	internal void NotifyPopupIsClosed()
	{
		Closed?.Invoke(this, EventArgs.Empty);
	}

	PopupContainer GetPopupContainer()
	{
		var parent = Parent;

		while (parent is not null)
		{
			if (parent.Parent is PopupContainer popupContainer)
			{
				return popupContainer;
			}

			parent = parent.Parent;
		}
		
		throw new InvalidOperationException($"Unable to close popup: could not locate {nameof(PopupContainer)}. If using a custom implementation of {nameof(Popup)}, override the {nameof(Close)} method");
	}
}

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
public partial class Popup<T> : Popup
{
	/// <summary>
	/// Close the Popup with a result.
	/// </summary>
	/// <param name="result">Popup result</param>
	public virtual Task Close(T result) =>  GetPopupContainer().Close(new PopupResult<T>(result, false));
	
	PopupContainer<T> GetPopupContainer()
	{
		var parent = Parent;
		
		while (parent is not null)
		{
			if (parent.Parent is PopupContainer<T> popupContainer)
			{
				return popupContainer;
			}

			parent = parent.Parent;
		}
		
		throw new InvalidOperationException($"Unable to close popup: could not locate {nameof(PopupContainer)}. If using a custom implementation of {nameof(Popup)}, override the {nameof(Close)} method");
	}
}