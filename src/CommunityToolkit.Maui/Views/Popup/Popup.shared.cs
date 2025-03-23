using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
public partial class Popup : ContentView
{
	/// <remarks>Use <see cref="PopupOptions.BackgroundColorProperty"/> to set the <see cref="BackgroundColorProperty"/> for <see cref="Popup"/></remarks>
	[Obsolete($"Use {nameof(PopupOptions)} to bind to the BackgroundColorProperty of {nameof(Popup)}", true)]
	public static new readonly BindableProperty BackgroundColorProperty = VisualElement.BackgroundColorProperty;

	/// <remarks>Use <see cref="PopupOptions.BackgroundColorProperty"/> to set the <see cref="BackgroundProperty"/> for <see cref="Popup"/></remarks>
	[Obsolete($"Use {nameof(PopupOptions)} to bind to the BackgroundProperty of {nameof(Popup)}", true)]
	public static new readonly BindableProperty BackgroundProperty = VisualElement.BackgroundProperty;

	/// <remarks>Use <see cref="PopupOptions.MarginProperty"/> to set the <see cref="MarginProperty"/> for <see cref="Popup"/></remarks>
	[Obsolete($"Use {nameof(PopupOptions)} to bind to the MarginProperty of {nameof(Popup)}", true)]
	public static new readonly BindableProperty MarginProperty = View.MarginProperty;

	/// <remarks>Use <see cref="PopupOptions.PaddingProperty"/> to set the <see cref="PaddingProperty"/> for <see cref="Popup"/></remarks>
	[Obsolete($"Use {nameof(PopupOptions)} to bind to the PaddingProperty of {nameof(Popup)}", true)]
	public static new readonly BindableProperty PaddingProperty = Microsoft.Maui.Controls.Compatibility.Layout.PaddingProperty;

	/// <remarks>Use <see cref="PopupOptions.VerticalOptionsProperty"/> to set the <see cref="VerticalOptionsProperty"/> for <see cref="Popup"/></remarks>
	[Obsolete($"Use {nameof(PopupOptions)} to bind to the VerticalOptionsProperty  {nameof(Popup)}", true)]
	public static new readonly BindableProperty VerticalOptionsProperty = View.VerticalOptionsProperty;

	/// <remarks>Use <see cref="PopupOptions.HorizontalOptionsProperty"/> to set the <see cref="HorizontalOptionsProperty"/> for <see cref="Popup"/></remarks>
	[Obsolete($"Use {nameof(PopupOptions)} to bind to the HorizontalOptionsProperty of {nameof(Popup)}", true)]
	public static new readonly BindableProperty HorizontalOptionsProperty = View.HorizontalOptionsProperty;

	/// <summary>
	/// Event occurs when the Popup is opened.
	/// </summary>
	public event EventHandler? Opened;

	/// <summary>
	/// Event occurs when the Popup is closed.
	/// </summary>
	public event EventHandler? Closed;

	/// <remarks>Use <see cref="PopupOptions.BackgroundColor"/> to set the <see cref="BackgroundColor"/> for <see cref="Popup"/></remarks>
	public new Color BackgroundColor => base.BackgroundColor;

	/// <remarks>Use <see cref="PopupOptions.BackgroundColor"/> to set the <see cref="Background"/> for <see cref="Popup"/></remarks>
	public new Brush Background => base.Background;

	/// <remarks>Use <see cref="PopupOptions.Margin"/> to set the <see cref="Margin"/> for <see cref="Popup"/></remarks>
	public new Thickness Margin => base.Margin;

	/// <remarks>Use <see cref="PopupOptions.Padding"/> to set the <see cref="Padding"/> for <see cref="Popup"/></remarks>
	public new Thickness Padding => base.Padding;

	/// <remarks>Use <see cref="PopupOptions.VerticalOptions"/> to set the <see cref="VerticalOptions"/> for <see cref="Popup"/></remarks>
	public new LayoutOptions VerticalOptions => base.VerticalOptions;

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

	private protected PopupContainer GetPopupContainer()
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

		throw new InvalidOperationException($"Unable to close popup: could not locate {nameof(PopupContainer)}. {nameof(PopupExtensions.ShowPopup)} or {nameof(PopupExtensions.ShowPopupAsync)} must be called before {Close()}. If using a custom implementation of {nameof(Popup)}, override the {nameof(Close)} method");
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
}