using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
public partial class Popup : ContentView
{
	/// <summary>
	///  Backing BindableProperty for the <see cref="CanBeDismissedByTappingOutsideOfPopup"/> property.
	/// </summary>
	public static readonly BindableProperty CanBeDismissedByTappingOutsideOfPopupProperty = BindableProperty.Create(nameof(CanBeDismissedByTappingOutsideOfPopup), typeof(bool), typeof(Popup), true);
	
	/// <summary>
	///  Backing BindableProperty for the <see cref="OnTappingOutsideOfPopup"/> property.
	/// </summary>
	public static readonly BindableProperty OnTappingOutsideOfPopupProperty = BindableProperty.Create(nameof(OnTappingOutsideOfPopup), typeof(Action), typeof(Popup), null);
	
	readonly WeakEventManager weakEventManager = new();
	
	/// <summary>
	/// Event occurs when the Popup is opened.
	/// </summary>
	public event EventHandler Opened
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Event occurs when the Popup is closed.
	/// </summary>
	public event EventHandler Closed
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Gets or sets a value indicating whether the popup can be dismissed by tapping outside the Popup.
	/// </summary>
	/// <remarks>
	/// When true and the user taps outside the popup, it will dismiss.
	/// On Android - when false the hardware back button is disabled.
	/// </remarks>
	public bool CanBeDismissedByTappingOutsideOfPopup
	{
		get => (bool)GetValue(CanBeDismissedByTappingOutsideOfPopupProperty);
		set => SetValue(CanBeDismissedByTappingOutsideOfPopupProperty, value);
	}

	/// <summary>
	/// Action to be executed when the user taps outside the Popup.
	/// </summary>
	public Action? OnTappingOutsideOfPopup
	{
		get => (Action?)GetValue(OnTappingOutsideOfPopupProperty);
		set => SetValue(OnTappingOutsideOfPopupProperty, value);
	}

	/// <summary>
	/// Close the Popup.
	/// </summary>
	public virtual Task Close() => GetPopupContainer().Close(new PopupResult(false));

	internal void NotifyPopupIsOpened()
	{
		weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(Opened));
	}

	internal void NotifyPopupIsClosed()
	{
		weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(Closed));
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
		
		throw new InvalidOperationException($"Unable to close popup: could not locate {nameof(PopupContainer<T>)}. If using a custom implementation of {nameof(Popup)}, override the {nameof(Close)} method");
	}
}