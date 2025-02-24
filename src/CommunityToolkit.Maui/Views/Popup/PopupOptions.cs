namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Popup options.
/// </summary>
public partial class PopupOptions : BindableObject
{
	/// <summary>
	///  Backing BindableProperty for the <see cref="CanBeDismissedByTappingOutsideOfPopup"/> property.
	/// </summary>
	public static readonly BindableProperty CanBeDismissedByTappingOutsideOfPopupProperty = BindableProperty.Create(nameof(CanBeDismissedByTappingOutsideOfPopup), typeof(bool), typeof(PopupOptions), true);

	/// <summary>
	///  Backing BindableProperty for the <see cref="OnTappingOutsideOfPopup"/> property.
	/// </summary>
	public static readonly BindableProperty OnTappingOutsideOfPopupProperty = BindableProperty.Create(nameof(OnTappingOutsideOfPopup), typeof(Action), typeof(PopupOptions), null);

	/// <summary>
	///  Backing BindableProperty for the <see cref="BackgroundColor"/> property.
	/// </summary>
	public static readonly BindableProperty BackgroundColorProperty = BindableProperty.Create(nameof(BackgroundColor), typeof(Color), typeof(PopupOptions), Color.FromRgba(0, 0, 0, 0.4));

	/// <summary>
	///  Backing BindableProperty for the <see cref="Shape"/> property.
	/// </summary>
	public static readonly BindableProperty ShapeProperty = BindableProperty.Create(nameof(Shape), typeof(IShape), typeof(PopupOptions), null);

	/// <summary>
	///  Backing BindableProperty for the <see cref="Margin"/> property.
	/// </summary>
	public static readonly BindableProperty MarginProperty = BindableProperty.Create(nameof(Margin), typeof(Thickness), typeof(PopupOptions), new Thickness(30));

	/// <summary>
	///  Backing BindableProperty for the <see cref="Padding"/> property.
	/// </summary>
	public static readonly BindableProperty PaddingProperty = BindableProperty.Create(nameof(Padding), typeof(Thickness), typeof(PopupOptions), new Thickness(15));

	/// <summary>
	///  Backing BindableProperty for the <see cref="VerticalOptions"/> property.
	/// </summary>
	public static readonly BindableProperty VerticalOptionsProperty = BindableProperty.Create(nameof(VerticalOptions), typeof(LayoutOptions), typeof(PopupOptions), LayoutOptions.Center);

	/// <summary>
	///  Backing BindableProperty for the <see cref="HorizontalOptions"/> property.
	/// </summary>
	public static readonly BindableProperty HorizontalOptionsProperty = BindableProperty.Create(nameof(HorizontalOptions), typeof(LayoutOptions), typeof(PopupOptions), LayoutOptions.Center);

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
	/// Popup background color.
	/// </summary>
	public Color BackgroundColor
	{
		get => (Color)GetValue(BackgroundColorProperty);
		set => SetValue(BackgroundColorProperty, value);
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
	/// Popup border shape.
	/// </summary>
	public IShape? Shape
	{
		get => (IShape?)GetValue(ShapeProperty);
		set => SetValue(ShapeProperty, value);
	}

	/// <summary>
	/// Popup margin.
	/// </summary>
	public Thickness Margin
	{
		get => (Thickness)GetValue(MarginProperty);
		set => SetValue(MarginProperty, value);
	}

	/// <summary>
	/// Popup padding.
	/// </summary>
	public Thickness Padding
	{
		get => (Thickness)GetValue(PaddingProperty);
		set => SetValue(PaddingProperty, value);
	}

	/// <summary>
	/// Popup vertical options.
	/// </summary>
	public LayoutOptions VerticalOptions
	{
		get => (LayoutOptions)GetValue(VerticalOptionsProperty);
		set => SetValue(VerticalOptionsProperty, value);
	}

	/// <summary>
	/// Popup horizontal options.
	/// </summary>
	public LayoutOptions HorizontalOptions
	{
		get => (LayoutOptions)GetValue(HorizontalOptionsProperty);
		set => SetValue(HorizontalOptionsProperty, value);
	}
}