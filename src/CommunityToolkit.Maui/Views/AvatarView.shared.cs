using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views;

/// <summary>AvatarView control.</summary>
public partial class AvatarView : Border, IAvatarView
{
	/// <summary>The backing store for the <c>FontAttributes</c> bindable property.</summary>
	public static readonly BindableProperty FontAttributesProperty = BindableProperty.Create(
		nameof(FontAttributes),
		typeof(FontAttributes),
		typeof(AvatarView),
		FontAttributes.None,
		propertyChanged: static (bindable, oldValue, newValue) =>
			((AvatarView)bindable).OnFontAttributesChanged((FontAttributes)oldValue, (FontAttributes)newValue));

	/// <summary>The backing store for the <c>FontAutoScalingEnabled</c> bindable property.</summary>
	public static readonly BindableProperty FontAutoScalingEnabledProperty = BindableProperty.Create(
		nameof(FontAutoScalingEnabled),
		typeof(bool),
		typeof(AvatarView),
		true,
		propertyChanged: static (bindable, oldValue, newValue) =>
			((AvatarView)bindable).OnFontAutoScalingEnabledChanged((bool)oldValue, (bool)newValue));

	/// <summary>The backing store for the <c>FontFamily</c> bindable property.</summary>
	public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
		nameof(FontFamily),
		typeof(string),
		typeof(AvatarView),
		default(string),
		propertyChanged: static (bindable, oldValue, newValue) =>
			((AvatarView)bindable).OnFontFamilyChanged((string?)oldValue, (string?)newValue));

	/// <summary>The backing store for the <c>FontSize</c> bindable property.</summary>
	public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
		nameof(FontSize),
		typeof(double),
		typeof(AvatarView),
		-1d,
		propertyChanged: static (bindable, oldValue, newValue) =>
			((AvatarView)bindable).OnFontSizeChanged((double)oldValue, (double)newValue));

	/// <summary>The backing store for the <see cref="ITextStyle.TextColor" /> bindable property.</summary>
	public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
		nameof(TextColor),
		typeof(Color),
		typeof(AvatarView),
		default(Color),
		propertyChanged: static (bindable, oldValue, newValue) =>
			((AvatarView)bindable).OnTextColorPropertyChanged((Color?)oldValue, (Color?)newValue));

	/// <summary>The backing store for the <see cref="CharacterSpacing" /> bindable property.</summary>
	public static readonly BindableProperty CharacterSpacingProperty = BindableProperty.Create(
		nameof(CharacterSpacing),
		typeof(double),
		typeof(AvatarView),
		0d,
		propertyChanged: static (bindable, oldValue, newValue) =>
			((AvatarView)bindable).OnCharacterSpacingPropertyChanged((double)oldValue, (double)newValue));

	/// <summary>The backing store for the <see cref="TextTransform" /> bindable property.</summary>
	public static readonly BindableProperty TextTransformProperty = BindableProperty.Create(
		nameof(TextTransform),
		typeof(TextTransform),
		typeof(AvatarView),
		TextTransform.Default,
		propertyChanged: static (bindable, oldValue, newValue) =>
			((AvatarView)bindable).OnTextTransformChanged((TextTransform)oldValue, (TextTransform)newValue));


	readonly Image avatarImage = new()
	{
		Aspect = Aspect.AspectFill,
	};

	readonly Label avatarLabel = new()
	{
		HorizontalTextAlignment = TextAlignment.Center,
		VerticalTextAlignment = TextAlignment.Center,
		Text = AvatarViewDefaults.Text,
	};

	bool wasImageLoading;

	/// <summary>
	/// Initializes a new instance of the <see cref="AvatarView"/> class.
	/// </summary>
	public AvatarView()
	{
		PropertyChanged += HandlePropertyChanged;

		IsEnabled = true;
		HorizontalOptions = VerticalOptions = LayoutOptions.Center;
		HeightRequest = AvatarViewDefaults.HeightRequest;
		WidthRequest = AvatarViewDefaults.WidthRequest;
		Padding = AvatarViewDefaults.Padding;
		Stroke = AvatarViewDefaults.BorderColor;
		StrokeThickness = AvatarViewDefaults.BorderWidth;
		StrokeShape = new RoundRectangle
		{
			CornerRadius = new CornerRadius(AvatarViewDefaults.CornerRadius.TopLeft, AvatarViewDefaults.CornerRadius.TopRight, AvatarViewDefaults.CornerRadius.BottomLeft, AvatarViewDefaults.CornerRadius.BottomRight),
		};
		Content = avatarLabel;
		avatarImage.SetBinding(WidthRequestProperty, BindingBase.Create<VisualElement, double>(static p => p.WidthRequest, source: this));
		avatarImage.SetBinding(HeightRequestProperty, BindingBase.Create<VisualElement, double>(static p => p.HeightRequest, source: this));
	}

	/// <summary>
	/// Gets or sets the control font.
	/// </summary>
	public Microsoft.Maui.Font Font { get; set; } = Microsoft.Maui.Font.SystemFontOfSize((double)FontSizeProperty.DefaultValue);

	/// <summary>
	/// Gets or sets a value of the control text character spacing property.
	/// </summary>
	public double CharacterSpacing
	{
		get => (double)GetValue(CharacterSpacingProperty);
		set => SetValue(CharacterSpacingProperty, value);
	}

	/// <summary>Gets or sets a value of the control font attributes property.</summary>
	public FontAttributes FontAttributes
	{
		get => (FontAttributes)GetValue(FontAttributesProperty);
		set => SetValue(FontAttributesProperty, value);
	}

	/// <summary>Gets or sets a value indicating whether control font auto scaling enabled property.</summary>
	public bool FontAutoScalingEnabled
	{
		get => (bool)GetValue(FontAutoScalingEnabledProperty);
		set => SetValue(FontAutoScalingEnabledProperty, value);
	}

	/// <summary>Gets or sets a value of the control font family property.</summary>
	public string? FontFamily
	{
		get => (string?)GetValue(FontFamilyProperty);
		set => SetValue(FontFamilyProperty, value);
	}

	/// <summary>Gets or sets a value of the control font size property.</summary>
	[TypeConverter(typeof(FontSizeConverter))]
	public double FontSize
	{
		get => (double)GetValue(FontSizeProperty);
		set => SetValue(FontSizeProperty, value);
	}

	/// <summary>Gets or sets a value of the control text colour property.</summary>
	public Color TextColor
	{
		get => (Color)GetValue(TextColorProperty);
		set => SetValue(TextColorProperty, value);
	}

	/// <inheritdoc/>
	public TextTransform TextTransform
	{
		get => (TextTransform)GetValue(TextTransformProperty);
		set => SetValue(TextTransformProperty, value);
	}

	/// <summary>Gets or sets a value of the control border colour.</summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnBorderColorPropertyChanged))]
	public partial Color BorderColor { get; set; } = AvatarViewDefaults.BorderColor;

	/// <summary>
	/// Gets or sets a value of the control border width.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnBorderWidthPropertyChanged))]
	public partial double BorderWidth { get; set; } = AvatarViewDefaults.BorderWidth;

	/// <summary>
	/// Gets or sets a value of the control corner radius property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnCornerRadiusPropertyChanged))]
	public partial CornerRadius CornerRadius { get; set; } = AvatarViewDefaults.CornerRadius;

	/// <summary>
	/// Gets or sets a value of the control image source property.
	/// </summary>
	[TypeConverter(typeof(ImageSourceConverter))]
	[BindableProperty(PropertyChangedMethodName = nameof(OnImageSourcePropertyChanged))]
	public partial ImageSource? ImageSource { get; set; }

	/// <summary>
	/// Gets or sets a value of the control text property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnTextPropertyChanged))]
	public partial string Text { get; set; } = AvatarViewDefaults.Text;

	Aspect Microsoft.Maui.IImage.Aspect => avatarImage.Aspect;

	TextAlignment ITextAlignment.HorizontalTextAlignment => avatarLabel.HorizontalTextAlignment;

	bool IImageSource.IsEmpty => avatarImage.Source is null;

	bool IImageSourcePart.IsAnimationPlaying => avatarImage.IsAnimationPlaying;

	bool Microsoft.Maui.IImage.IsOpaque => avatarImage.IsOpaque;

	double ILabel.LineHeight => avatarLabel.LineHeight;

	IImageSource? IImageSourcePart.Source => ImageSource;

	TextAlignment ITextAlignment.VerticalTextAlignment => avatarLabel.VerticalTextAlignment;

	TextDecorations ILabel.TextDecorations => avatarLabel.TextDecorations;

	void OnCharacterSpacingPropertyChanged(double oldValue, double newValue)
	{
		InvalidateMeasure();
		avatarLabel.CharacterSpacing = newValue;
	}

	void OnFontAttributesChanged(FontAttributes oldValue, FontAttributes newValue)
	{
		HandleFontChanged();
		avatarLabel.FontAttributes = newValue;
	}

	void OnFontAutoScalingEnabledChanged(bool oldValue, bool newValue)
	{
		HandleFontChanged();
		avatarLabel.FontAutoScalingEnabled = newValue;
	}

	void OnFontFamilyChanged(string? oldValue, string? newValue)
	{
		HandleFontChanged();
		avatarLabel.FontFamily = newValue;
	}

	void OnFontSizeChanged(double oldValue, double newValue)
	{
		HandleFontChanged();
		avatarLabel.FontSize = newValue;
	}

	void OnTextColorPropertyChanged(Color? oldValue, Color? newValue) => avatarLabel.TextColor = newValue;

	void OnTextTransformChanged(TextTransform oldValue, TextTransform newValue)
	{
		InvalidateMeasure();
		avatarLabel.TextTransform = newValue;
	}

	void IImageSourcePart.UpdateIsLoading(bool isLoading)
	{
		if (!isLoading && wasImageLoading)
		{
			Handler?.UpdateValue(nameof(AvatarView));
		}

		wasImageLoading = isLoading;
	}

	static void OnBorderColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		AvatarView avatarView = (AvatarView)bindable;
		avatarView.Stroke = (Color)newValue;
	}

	static void OnBorderWidthPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		AvatarView avatarView = (AvatarView)bindable;
		avatarView.StrokeThickness = (double)newValue;
	}

	static void OnCornerRadiusPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		AvatarView avatarView = (AvatarView)bindable;
		CornerRadius corderRadius = (CornerRadius)newValue;

		avatarView.StrokeShape = new RoundRectangle
		{
			CornerRadius = corderRadius
		};
	}

	static void OnImageSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		AvatarView avatarView = (AvatarView)bindable;
		avatarView.HandleImageChanged((ImageSource?)newValue);
	}

	static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		AvatarView avatarView = (AvatarView)bindable;
		avatarView.avatarLabel.Text = (string)newValue;
	}

	void HandleFontChanged()
	{
		Font = Microsoft.Maui.Font.OfSize(
			FontFamily,
			FontSize,
			FontAttributes.HasFlag(FontAttributes.Bold) ? FontWeight.Bold : FontWeight.Regular,
			FontAttributes.HasFlag(FontAttributes.Italic) ? FontSlant.Italic : FontSlant.Default,
			FontAutoScalingEnabled);
		Handler?.UpdateValue(nameof(ITextStyle.Font));
		InvalidateMeasure();
	}

	void HandleImageChanged(ImageSource? newValue)
	{
		avatarImage.Source = newValue;
		if (newValue is not null)
		{
			RefreshAvatarImage();
			Content = avatarImage;
		}
		else
		{
			Content = avatarLabel;
		}
	}

	void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		// Ensure avatarImage is clipped to the bounds of the AvatarView whenever its Height, Width, CornerRadius, Border, StrokeThickness and Padding properties change
		if (e.PropertyName == HeightProperty.PropertyName
			|| e.PropertyName == WidthProperty.PropertyName
			|| e.PropertyName == PaddingProperty.PropertyName
			|| e.PropertyName == BorderWidthProperty.PropertyName
			|| e.PropertyName == CornerRadiusProperty.PropertyName
			|| e.PropertyName == StrokeThicknessProperty.PropertyName
		   )
		{
			RefreshAvatarImage();
		}
	}

	void RefreshAvatarImage()
	{
		if (Height >= 0 // The default value of Height (before the view is drawn onto the page) is -1
			&& Width >= 0 // The default value of Width (before the view is drawn onto the page) is -1
			&& avatarImage.Source is not null)
		{
			Geometry? avatarImageClipGeometry = null;
#if WINDOWS
			double offsetX = 0;
			double offsetY = 0;
#else
			double offsetX = StrokeThickness + Padding.Left;
			double offsetY = StrokeThickness + Padding.Top;
#endif
			double imageWidth = Width - (StrokeThickness * 2) - Padding.Left - Padding.Right;
			double imageHeight = Height - (StrokeThickness * 2) - Padding.Top - Padding.Bottom;
			avatarImage.WidthRequest = imageWidth;
			avatarImage.HeightRequest = imageHeight;

			if (!OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst() && !OperatingSystem.IsMacOS())
			{
				avatarImageClipGeometry = new RoundRectangleGeometry
				{
					CornerRadius = CornerRadius,
					Rect = new(offsetX, offsetY, imageWidth, imageHeight)
				};
			}

			avatarImage.Clip = StrokeShape switch
			{
				Polyline polyLine => polyLine.Clip,
				Ellipse ellipse => ellipse.Clip,
				Microsoft.Maui.Controls.Shapes.Path path => path.Clip,
				Polygon polygon => polygon.Clip,
				Rectangle rectangle => rectangle.Clip,
				RoundRectangle roundRectangle => roundRectangle.Clip,
				_ => avatarImageClipGeometry
			};
		}
	}
}