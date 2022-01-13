using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// The popup control's base implementation.
/// </summary>
[ContentProperty(nameof(Content))]
public abstract class BasePopup : Element, IElementConfiguration<BasePopup>, IBasePopup
{
	readonly WeakEventManager dismissWeakEventManager = new();
	readonly WeakEventManager openedWeakEventManager = new();

	/// <summary>
	/// Instantiates a new instance of <see cref="BasePopup"/>.
	/// </summary>
	protected BasePopup()
	{
		VerticalOptions = LayoutAlignment.Center;
		HorizontalOptions = LayoutAlignment.Center;
		IsLightDismissEnabled = true;
		platformConfigurationRegistry = new Lazy<PlatformConfigurationRegistry<BasePopup>>(() => new(this));
	}

	readonly Lazy<PlatformConfigurationRegistry<BasePopup>> platformConfigurationRegistry;

	public IPlatformElementConfiguration<T, BasePopup> On<T>() where T : IConfigPlatform =>
		platformConfigurationRegistry.Value.On<T>();

	public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(BasePopup), propertyChanged: OnContentChanged);

	public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(Color), typeof(BasePopup), default);
	public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(Size), typeof(BasePopup), default(Size));

	public static readonly BindableProperty VerticalOptionsProperty = BindableProperty.Create(nameof(VerticalOptions), typeof(LayoutAlignment), typeof(BasePopup), LayoutAlignment.Center);
	
	public static readonly BindableProperty HorizontalOptionsProperty = BindableProperty.Create(nameof(HorizontalOptions), typeof(LayoutAlignment), typeof(BasePopup), LayoutAlignment.Center);

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
	public bool IsLightDismissEnabled { get; set; }

	IView? IBasePopup.Anchor => Anchor;

	IView? IBasePopup.Content => Content;


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

	/// <summary>
	/// Invokes the <see cref="Dismissed"/> event.
	/// </summary>
	/// <param name="result">
	/// The results to add to the <see cref="PopupDismissedEventArgs"/>.
	/// </param>
	protected void OnDismissed(object? result)
	{
		(this as IBasePopup)?.OnDismissed(result);
		dismissWeakEventManager.HandleEvent(this, new PopupDismissedEventArgs(result, false), nameof(Dismissed));
	}

	/// <summary>
	/// Invokes the <see cref="Opened"/> event.
	/// </summary>
	internal virtual void OnOpened() =>
		openedWeakEventManager.HandleEvent(this, PopupOpenedEventArgs.Empty, nameof(Opened));

	/// <summary>
	/// Invoked when the popup is light dismissed. In other words when the
	/// user taps outside of the popup and it closes.
	/// </summary>
	protected internal virtual void LightDismiss() =>
		dismissWeakEventManager.HandleEvent(this, new PopupDismissedEventArgs(null, true), nameof(Dismissed));

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
		if (bindable is BasePopup popup)
		{
			popup.OnBindingContextChanged();
		}
	}

	void IBasePopup.OnDismissed(object? result)
	{
		Handler.Invoke(nameof(IBasePopup.OnDismissed), result);
	}

	void IBasePopup.OnOpened()
	{
		OnOpened();
	}

	void IBasePopup.LightDismiss()
	{
		LightDismiss();
	}
}