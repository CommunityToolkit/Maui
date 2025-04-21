using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
public partial class Popup : ContentView
{

	/// <summary>
	/// Initializes Popup
	/// </summary>
	public Popup()
	{
		Margin = PopupDefaults.Margin;
		Padding = PopupDefaults.Padding;
		HorizontalOptions = PopupDefaults.HorizontalOptions;
		VerticalOptions = PopupDefaults.VerticalOptions;
	}
	
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
	public virtual Task Close(CancellationToken token = default) => GetPopupPage().Close(new PopupResult(false), token);

	internal void NotifyPopupIsOpened()
	{
		Opened?.Invoke(this, EventArgs.Empty);
	}

	internal void NotifyPopupIsClosed()
	{
		Closed?.Invoke(this, EventArgs.Empty);
	}

	private protected PopupPage GetPopupPage()
	{
		var parent = Parent;

		while (parent is not null)
		{
			if (parent.Parent is PopupPage popuppage)
			{
				return popuppage;
			}

			parent = parent.Parent;
		}

		throw new InvalidOperationException($"Unable to close popup: could not locate {nameof(PopupPage)}. {nameof(PopupExtensions.ShowPopup)} or {nameof(PopupExtensions.ShowPopupAsync)} must be called before {nameof(Close)}. If using a custom implementation of {nameof(Popup)}, override the {nameof(Close)} method");
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
	/// <param name="token"><see cref="CancellationToken"/></param>
	public virtual Task Close(T result, CancellationToken token = default) => GetPopupPage().Close(new PopupResult<T>(result, false), token);
}