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

	/// <inheritdoc/>
	/// <remarks>Use <see cref="PopupOptions.BackgroundColor"/> to set the <see cref="BackgroundColor"/> for <see cref="Popup"/></remarks>
	public new Color BackgroundColor => base.BackgroundColor;

	/// <inheritdoc/>
	/// <remarks>Use <see cref="PopupOptions.Margin"/> to set the <see cref="Margin"/> for <see cref="Popup"/></remarks>
	public new Thickness Margin => base.Margin;

	/// <inheritdoc/>
	/// <remarks>Use <see cref="PopupOptions.Padding"/> to set the <see cref="Padding"/> for <see cref="Popup"/></remarks>
	public new Thickness Padding => base.Padding;

	/// <inheritdoc/>
	/// <remarks>Use <see cref="PopupOptions.VerticalOptions"/> to set the <see cref="VerticalOptions"/> for <see cref="Popup"/></remarks>
	public new LayoutOptions VerticalOptions => base.VerticalOptions;

	/// <inheritdoc/>
	/// <remarks>Use <see cref="PopupOptions.HorizontalOptions"/> to set the <see cref="HorizontalOptions"/> for <see cref="Popup"/></remarks>
	public new LayoutOptions HorizontalOptions => base.HorizontalOptions;

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
	public virtual Task Close(T result) => GetPopupContainer().Close(new PopupResult<T>(result, false));

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

		throw new InvalidOperationException($"Unable to close popup: could not locate {nameof(PopupContainer<T>)}. If using a custom implementation of {nameof(Popup)}, override the {nameof(Close)} method");
	}
}