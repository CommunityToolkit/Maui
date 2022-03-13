using CommunityToolkit.Maui.Core;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a small View that pops up at front the Page. Implements <see cref="IPopup"/>.
/// </summary>
[ContentProperty(nameof(Content))]
public partial class Popup : Element, IPopup
{
	/// <summary>
	///  Backing BindableProperty for the <see cref="Content"/> property.
	/// </summary>
	public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(Popup), propertyChanged: OnContentChanged);

	/// <summary>
	///  Backing BindableProperty for the <see cref="Color"/> property.
	/// </summary>
	public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(Color), typeof(Popup), Colors.LightGray);

	/// <summary>
	///  Backing BindableProperty for the <see cref="Size"/> property.
	/// </summary>
	public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(Size), typeof(Popup), default(Size));

	/// <summary>
	///  Backing BindableProperty for the <see cref="CanBeDismissedByTappingOutsideOfPopup"/> property.
	/// </summary>
	public static readonly BindableProperty CanBeDismissedByTappingOutsideOfPopupProperty = BindableProperty.Create(nameof(CanBeDismissedByTappingOutsideOfPopup), typeof(bool), typeof(Popup), true);

	/// <summary>
	///  Backing BindableProperty for the <see cref="VerticalOptions"/> property.
	/// </summary>
	public static readonly BindableProperty VerticalOptionsProperty = BindableProperty.Create(nameof(VerticalOptions), typeof(LayoutAlignment), typeof(Popup), LayoutAlignment.Center);

	/// <summary>
	///  Backing BindableProperty for the <see cref="HorizontalOptions"/> property.
	/// </summary>
	public static readonly BindableProperty HorizontalOptionsProperty = BindableProperty.Create(nameof(HorizontalOptions), typeof(LayoutAlignment), typeof(Popup), LayoutAlignment.Center);

	readonly WeakEventManager dismissWeakEventManager = new();
	readonly WeakEventManager openedWeakEventManager = new();
	readonly Lazy<PlatformConfigurationRegistry<Popup>> platformConfigurationRegistry;

	TaskCompletionSource<object?> taskCompletionSource = new();

	/// <summary>
	/// Instantiates a new instance of <see cref="Popup"/>.
	/// </summary>
	public Popup()
	{
		platformConfigurationRegistry = new Lazy<PlatformConfigurationRegistry<Popup>>(() => new(this));

		VerticalOptions = HorizontalOptions = LayoutAlignment.Center;

#if WINDOWS
		this.HandlerChanged += OnPopupHandlerChanged;
#endif
	}

	/// <summary>
	/// Dismissed event is invoked when the popup is closed.
	/// </summary>
	public event EventHandler<PopupClosedEventArgs> Closed
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

	/// <summary>
	/// Gets the final result of the dismissed popup.
	/// </summary>
	public Task<object?> Result => taskCompletionSource.Task;

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
	/// Gets or sets a value indicating whether the popup can be dismissed by tapping outside of the Popup.
	/// </summary>
	/// <remarks>
	/// When true and the user taps outside of the popup it will dismiss.
	/// On Android - when false the hardware back button is disabled.
	/// </remarks>
	public bool CanBeDismissedByTappingOutsideOfPopup
	{
		get => (bool)GetValue(CanBeDismissedByTappingOutsideOfPopupProperty);
		set => SetValue(CanBeDismissedByTappingOutsideOfPopupProperty, value);
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
	/// Gets or sets the result that will return when user taps outside of the Popup.
	/// </summary>
	protected object? ResultWhenUserTapsOutsideOfPopup { get; set; }

	/// <inheritdoc/>
	IView? IPopup.Anchor => Anchor;

	/// <inheritdoc/>
	IView? IPopup.Content => Content;

	/// <summary>
	/// Resets the Popup.
	/// </summary>
	public void Reset() => taskCompletionSource = new();

	/// <summary>
	/// Close the current popup.
	/// </summary>
	/// <param name="result">
	/// The result to return.
	/// </param>
	public void Close(object? result = null)
	{
		taskCompletionSource.TrySetResult(result);
		OnClosed(result, false);
	}

	/// <summary>
	/// Invokes the <see cref="Opened"/> event.
	/// </summary>
	internal virtual void OnOpened() =>
		openedWeakEventManager.HandleEvent(this, PopupOpenedEventArgs.Empty, nameof(Opened));

	/// <summary>
	/// Invokes the <see cref="Closed"/> event.
	/// </summary>
	/// <param name="result">
	/// Sets the <see cref="PopupClosedEventArgs"/> Property of <see cref="PopupClosedEventArgs.Result"/>.
	/// </param>
	/// /// <param name="wasDismissedByTappingOutsideOfPopup">
	/// Sets the <see cref="PopupClosedEventArgs"/> Property of <see cref="PopupClosedEventArgs.WasDismissedByTappingOutsideOfPopup"/>/>.
	/// </param>
	protected void OnClosed(object? result, bool wasDismissedByTappingOutsideOfPopup)
	{
		((IPopup)this).OnClosed(result);
		dismissWeakEventManager.HandleEvent(this, new PopupClosedEventArgs(result, wasDismissedByTappingOutsideOfPopup), nameof(Closed));
	}

	/// <summary>
	/// Invoked when the popup is dismissed by tapping outside of the popup.
	/// </summary>
	protected internal virtual void OnDismissedByTappingOutsideOfPopup()
	{
		taskCompletionSource.TrySetResult(ResultWhenUserTapsOutsideOfPopup);
		OnClosed(ResultWhenUserTapsOutsideOfPopup, true);
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

	void IPopup.OnClosed(object? result) => Handler.Invoke(nameof(IPopup.OnClosed), result);

	void IPopup.OnOpened() => OnOpened();

	void IPopup.OnDismissedByTappingOutsideOfPopup() => OnDismissedByTappingOutsideOfPopup();
}