using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui;

/// <summary>
/// Popup options.
/// </summary>
public partial class PopupOptions : BindableObject, IPopupOptions
{
	/// <summary>
	///  Backing BindableProperty for the <see cref="CanBeDismissedByTappingOutsideOfPopup"/> property.
	/// </summary>
	public static readonly BindableProperty CanBeDismissedByTappingOutsideOfPopupProperty = BindableProperty.Create(nameof(CanBeDismissedByTappingOutsideOfPopup), typeof(bool), typeof(PopupOptions), PopupOptionsDefaults.CanBeDismissedByTappingOutsideOfPopup);

	/// <summary>
	///  Backing BindableProperty for the <see cref="OnTappingOutsideOfPopup"/> property.
	/// </summary>
	public static readonly BindableProperty OnTappingOutsideOfPopupProperty = BindableProperty.Create(nameof(OnTappingOutsideOfPopup), typeof(Action), typeof(PopupOptions), PopupOptionsDefaults.OnTappingOutsideOfPopup);

	/// <summary>
	///  Backing BindableProperty for the <see cref="PageOverlayColor"/> property.
	/// </summary>
	public static readonly BindableProperty PageOverlayColorProperty = BindableProperty.Create(nameof(PageOverlayColor), typeof(Color), typeof(PopupOptions), PopupOptionsDefaults.PageOverlayColor);

	/// <summary>
	///  Backing BindableProperty for the <see cref="Shape"/> property.
	/// </summary>
	public static readonly BindableProperty ShapeProperty = BindableProperty.Create(nameof(Shape), typeof(Shape), typeof(PopupOptions), PopupOptionsDefaults.Shape);

	/// <summary>
	///  Backing BindableProperty for the <see cref="Margin"/> property.
	/// </summary>
	public static readonly BindableProperty MarginProperty = BindableProperty.Create(nameof(Margin), typeof(Thickness), typeof(PopupOptions), PopupOptionsDefaults.Margin);

	/// <summary>
	///  Backing BindableProperty for the <see cref="Padding"/> property.
	/// </summary>
	public static readonly BindableProperty PaddingProperty = BindableProperty.Create(nameof(Padding), typeof(Thickness), typeof(PopupOptions), PopupOptionsDefaults.Padding);

	/// <summary>
	///  Backing BindableProperty for the <see cref="VerticalOptions"/> property.
	/// </summary>
	public static readonly BindableProperty VerticalOptionsProperty = BindableProperty.Create(nameof(VerticalOptions), typeof(LayoutOptions), typeof(PopupOptions), PopupOptionsDefaults.VerticalOptions);

	/// <summary>
	///  Backing BindableProperty for the <see cref="HorizontalOptions"/> property.
	/// </summary>
	public static readonly BindableProperty HorizontalOptionsProperty = BindableProperty.Create(nameof(HorizontalOptions), typeof(LayoutOptions), typeof(PopupOptions), PopupOptionsDefaults.HorizontalOptions);

	/// <summary>
	///  Backing BindableProperty for the <see cref="HorizontalOptions"/> property.
	/// </summary>
	public static readonly BindableProperty ShadowProperty = BindableProperty.Create(nameof(Shadow), typeof(Shadow), typeof(PopupOptions), PopupOptionsDefaults.Shadow);

	/// <summary>
	/// An empty instance of <see cref="IPopupOptions"/> containing default values.
	/// </summary>
	public static IPopupOptions Empty { get; } = new PopupOptions();

	/// <inheritdoc/>
	/// <remarks>
	/// When true and the user taps outside the popup, it will dismiss.
	/// On Android - when false the hardware back button is disabled.
	/// </remarks>
	public bool CanBeDismissedByTappingOutsideOfPopup
	{
		get => (bool)GetValue(CanBeDismissedByTappingOutsideOfPopupProperty);
		set => SetValue(CanBeDismissedByTappingOutsideOfPopupProperty, value);
	}

	/// <inheritdoc/>
	public Color PageOverlayColor
	{
		get => (Color)GetValue(PageOverlayColorProperty);
		set => SetValue(PageOverlayColorProperty, value);
	}

	/// <inheritdoc/>
	public Action? OnTappingOutsideOfPopup
	{
		get => (Action?)GetValue(OnTappingOutsideOfPopupProperty);
		set => SetValue(OnTappingOutsideOfPopupProperty, value);
	}

	/// <inheritdoc/>
	public Shape? Shape
	{
		get => (Shape?)GetValue(ShapeProperty);
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

	/// <inheritdoc/>
	public Thickness Padding
	{
		get => (Thickness)GetValue(PaddingProperty);
		set => SetValue(PaddingProperty, value);
	}

	/// <inheritdoc/>
	public LayoutOptions VerticalOptions
	{
		get => (LayoutOptions)GetValue(VerticalOptionsProperty);
		set => SetValue(VerticalOptionsProperty, value);
	}

	/// <inheritdoc/>
	public LayoutOptions HorizontalOptions
	{
		get => (LayoutOptions)GetValue(HorizontalOptionsProperty);
		set => SetValue(HorizontalOptionsProperty, value);
	}

	/// <inheritdoc/>
	public Shadow? Shadow
	{
		get => (Shadow?)GetValue(ShadowProperty);
		set => SetValue(ShadowProperty, value);
	}
}