using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// The popup control's base implementation.
/// </summary>
[ContentProperty(nameof(Content))]
public partial class Popup : Element, IPopup
{
	TaskCompletionSource<object?> taskCompletionSource = new();

	/// <summary>
	/// Gets the final result of the dismissed popup.
	/// </summary>
	public Task<object?> Result => taskCompletionSource.Task;

	readonly WeakEventManager dismissWeakEventManager = new();
	readonly WeakEventManager openedWeakEventManager = new();
	readonly Lazy<PlatformConfigurationRegistry<Popup>> platformConfigurationRegistry;

	/// <summary>
	/// Instantiates a new instance of <see cref="Popup"/>.
	/// </summary>
	public Popup()
	{
		platformConfigurationRegistry = new Lazy<PlatformConfigurationRegistry<Popup>>(() => new(this));

		VerticalOptions = LayoutAlignment.Center;
		HorizontalOptions = LayoutAlignment.Center;
	}

	/// <summary>
	///  Backing BindableProperty for the <see cref="Content"/> property.
	/// </summary>
	public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(Popup), propertyChanged: OnContentChanged);

	/// <summary>
	///  Backing BindableProperty for the <see cref="Color"/> property.
	/// </summary>
	public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(Color), typeof(Popup), Colors.Transparent);

	/// <summary>
	///  Backing BindableProperty for the <see cref="Size"/> property.
	/// </summary>
	public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(Size), typeof(Popup), default(Size));

	/// <summary>
	///  Backing BindableProperty for the <see cref="IsLightDismissEnabled"/> property.
	/// </summary>
	public static readonly BindableProperty IsLightDismissEnabledProperty = BindableProperty.Create(nameof(IsLightDismissEnabled), typeof(bool), typeof(Popup), true);

	/// <summary>
	///  Backing BindableProperty for the <see cref="VerticalOptions"/> property.
	/// </summary>
	public static readonly BindableProperty VerticalOptionsProperty = BindableProperty.Create(nameof(VerticalOptions), typeof(LayoutAlignment), typeof(Popup), LayoutAlignment.Center);

	/// <summary>
	///  Backing BindableProperty for the <see cref="HorizontalOptions"/> property.
	/// </summary>
	public static readonly BindableProperty HorizontalOptionsProperty = BindableProperty.Create(nameof(HorizontalOptions), typeof(LayoutAlignment), typeof(Popup), LayoutAlignment.Center);

	/// <summary>
	/// Gets or sets the <see cref="View"/> content to render in the Popup.
	/// </summary>
	/// <remarks>
	/// The View can be or type: <see cref="View"/>, <see cref="ContentPage"/> or <see cref="NavigationPage"/>
	/// </remarks>
	public virtual View? Content
	{
		get => (View?)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="Color"/> of the Popup.
	/// </summary>
	/// <remarks>
	/// This color sets the native background color of the <see cref="Popup"/>, which is
	/// independent of any background color configured in the actual View.
	/// </remarks>
	public Color Color
	{
		get => (Color)GetValue(ColorProperty);
		set => SetValue(ColorProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="LayoutOptions"/> for positioning the <see cref="Popup"/> vertically on the screen.
	/// </summary>
	public LayoutAlignment VerticalOptions
	{
		get => (LayoutAlignment)GetValue(VerticalOptionsProperty);
		set => SetValue(VerticalOptionsProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="LayoutOptions"/> for positioning the <see cref="Popup"/> horizontally on the screen.
	/// </summary>
	public LayoutAlignment HorizontalOptions
	{
		get => (LayoutAlignment)GetValue(HorizontalOptionsProperty);
		set => SetValue(HorizontalOptionsProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="View"/> anchor.
	/// </summary>
	/// <remarks>
	/// The Anchor is where the Popup will render closest to. When an Anchor is configured
	/// the popup will appear centered over that control or as close as possible.
	/// </remarks>
	public View? Anchor { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="Size"/> of the Popup Display.
	/// </summary>
	/// <remarks>
	/// The Popup will always try to constrain the actual size of the <see cref="Popup" />
	/// to the <see cref="Popup" /> of the View unless a <see cref="Size"/> is specified.
	/// If the <see cref="Popup" /> contiains <see cref="LayoutOptions"/> a <see cref="Size"/>
	/// will be required. This will allow the View to have a concept of <see cref="Size"/>
	/// that varies from the actual <see cref="Size"/> of the <see cref="Popup" />
	/// </remarks>
	public Size Size
	{
		get => (Size)GetValue(SizeProperty);
		set => SetValue(SizeProperty, value);
	}

	/// <summary>
	/// Gets or sets a value indicating whether the popup can be light dismissed.
	/// </summary>
	/// <remarks>
	/// When true and the user taps outside of the popup it will dismiss.
	/// On Android - when false the hardware back button is disabled.
	/// </remarks>
	public bool IsLightDismissEnabled
	{
		get => (bool)GetValue(IsLightDismissEnabledProperty);
		set => SetValue(IsLightDismissEnabledProperty, value);
	}

	/// <summary>
	/// Dismissed event is invoked when the popup is closed.
	/// </summary>
	public event EventHandler<PopupDismissedEventArgs> Dismissed
	{
		add => dismissWeakEventManager.AddEventHandler(value);
		remove => dismissWeakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Opened event is invoked when the popup is opened.
	/// </summary>
	public event EventHandler<PopupOpenedEventArgs> Opened
	{
		add => openedWeakEventManager.AddEventHandler(value);
		remove => openedWeakEventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc/>
	IView? IPopup.Anchor => Anchor;

	/// <inheritdoc/>
	IView? IPopup.Content => Content;

	/// <summary>
	/// Resets the Popup.
	/// </summary>
	public void Reset() => taskCompletionSource = new();

	/// <summary>
	/// Displays a Popup and returns a result
	/// </summary>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException">Thrown when either <see cref="IMauiContext"/> or <see cref="Application.Current"/> is not initalized</exception>
	public Task<object?> ShowPopupAsync()
	{
		ShowPopup();

		return Result;
	}

	/// <summary>
	/// Displays a Popop
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when either <see cref="IMauiContext"/> or <see cref="Application.Current"/> is not initalized</exception>
	public void ShowPopup()
	{
		var mainPage = Application.Current?.MainPage ?? throw new InvalidOperationException($"{nameof(ShowPopupAsync)} requires {nameof(Application.Current.MainPage)} to be initiazlied");
		var mauiContext = Application.Current?.FindMauiContext() ?? throw new InvalidOperationException($"{nameof(ShowPopupAsync)} requires {nameof(IMauiContext)}");

		var currentPage = PageExtensions.GetCurrentPage(mainPage);
		Parent ??= PageExtensions.GetCurrentPage(mainPage);

		var popupNative = this.ToHandler(mauiContext);
		popupNative.Invoke(nameof(IPopup.OnOpened));
	}

	/// <summary>
	/// Dismiss the current popup.
	/// </summary>
	/// <param name="result">
	/// The result to return.
	/// </param>
	public void Dismiss(object? result)
	{
		taskCompletionSource.TrySetResult(result);
		OnDismissed(result);
	}

	/// <summary>
	/// Invokes the <see cref="Opened"/> event.
	/// </summary>
	internal virtual void OnOpened() =>
		openedWeakEventManager.HandleEvent(this, PopupOpenedEventArgs.Empty, nameof(Opened));

	/// <summary>
	/// Gets the light dismiss default result.
	/// </summary>
	/// <returns>
	/// The light dismiss value.
	/// </returns>
	/// <remarks>
	/// When a user dismisses the Popup via the light dismiss, this
	/// method will return a default value.
	/// </remarks>
	protected virtual object? GetLightDismissResult() => default;

	/// <summary>
	/// Invokes the <see cref="Dismissed"/> event.
	/// </summary>
	/// <param name="result">
	/// The results to add to the <see cref="PopupDismissedEventArgs"/>.
	/// </param>
	protected void OnDismissed(object? result)
	{
		((IPopup)this).OnDismissed(result);
		dismissWeakEventManager.HandleEvent(this, new PopupDismissedEventArgs(result, false), nameof(Dismissed));
	}

	/// <summary>
	/// Invoked when the popup is light dismissed. In other words when the
	/// user taps outside of the popup and it closes.
	/// </summary>
	protected internal virtual void LightDismiss()
	{
		taskCompletionSource.TrySetResult(GetLightDismissResult());
		dismissWeakEventManager.HandleEvent(this, new PopupDismissedEventArgs(null, true), nameof(Dismissed));
	}

	/// <summary>
	///<inheritdoc/>
	/// </summary>
	protected override void OnBindingContextChanged()
	{
		base.OnBindingContextChanged();

		if (Content is not null)
		{
			SetInheritedBindingContext(Content, BindingContext);
		}
	}

	static void OnContentChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var popup = (Popup)bindable;
		popup.OnBindingContextChanged();
	}

	void IPopup.OnDismissed(object? result) => Handler.Invoke(nameof(IPopup.OnDismissed), result);

	void IPopup.OnOpened() => OnOpened();

	void IPopup.LightDismiss() => LightDismiss();
}