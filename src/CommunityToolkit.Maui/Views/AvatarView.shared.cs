using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views;

/// <summary>AvatarView control.</summary>
public partial class AvatarView : Border, IAvatarView
{
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
	[BindableProperty(PropertyChangedMethodName = nameof(OnCharacterSpacingPropertyChanged))]
	public partial double CharacterSpacing { get; set; }

	/// <summary>Gets or sets a value of the control font attributes property.</summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnFontAttributesBindablePropertyChanged))]
	public partial FontAttributes FontAttributes { get; set; } = FontAttributes.None;

	/// <summary>Gets or sets a value indicating whether control font auto-scaling enabled property.</summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnFontAutoScalingEnabledPropertyChanged))]
	public partial bool FontAutoScalingEnabled { get; set; } = true;

	/// <summary>Gets or sets a value of the control font family property.</summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnFontFamilyPropertyChanged))]
	public partial string? FontFamily { get; set; }

	/// <summary>Gets or sets a value of the control font size property.</summary>
	[TypeConverter(typeof(FontSizeConverter))]
	[BindableProperty(PropertyChangedMethodName = nameof(OnFontSizePropertyChanged))]
	public partial double FontSize { get; set; } = -1d;

	/// <summary>Gets or sets a value of the control text colour property.</summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnTextColorPropertyChanged))]
	public partial Color TextColor { get; set; }

	/// <summary>Gets or sets a value of the control text transform property.</summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnTextTransformPropertyChanged))]
	public partial TextTransform TextTransform { get; set; } = TextTransform.Default;

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


	void OnCharacterSpacingChanged(double oldValue, double newValue)
	{
		InvalidateMeasure();
		avatarLabel.CharacterSpacing = newValue;
	}

	static void OnCharacterSpacingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var avatarView = (AvatarView)bindable;
		avatarView.OnCharacterSpacingChanged((double)oldValue, (double)newValue);
	}

	void OnFontAttributesChanged(FontAttributes oldValue, FontAttributes newValue)
	{
		HandleFontChanged();
		avatarLabel.FontAttributes = newValue;
	}

	static void OnFontAttributesBindablePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var avatarView = (AvatarView)bindable;
		avatarView.OnFontAttributesChanged((FontAttributes)oldValue, (FontAttributes)newValue);
	}

	void OnFontAutoScalingEnabledChanged(bool oldValue, bool newValue)
	{
		HandleFontChanged();
		avatarLabel.FontAutoScalingEnabled = newValue;
	}

	static void OnFontAutoScalingEnabledPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var avatarView = (AvatarView)bindable;
		avatarView.OnFontAutoScalingEnabledChanged((bool)oldValue, (bool)newValue);
	}

	static void OnFontFamilyPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var avatarView = (AvatarView)bindable;
		avatarView.OnFontFamilyChanged(oldValue as string, newValue as string);
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

	static void OnFontSizePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var avatarView = (AvatarView)bindable;
		avatarView.OnFontSizeChanged((double)oldValue, (double)newValue);
	}

	static void OnTextColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var avatarView = (AvatarView)bindable;
		avatarView.avatarLabel.TextColor = (Color?)newValue;
	}

	static void OnTextTransformPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var avatarView = (AvatarView)bindable;
		avatarView.InvalidateMeasure();
		avatarView.avatarLabel.TextTransform = (TextTransform)newValue;
	}

	static void OnBorderColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var avatarView = (AvatarView)bindable;
		avatarView.Stroke = (Color)newValue;
	}

	static void OnBorderWidthPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var avatarView = (AvatarView)bindable;
		avatarView.StrokeThickness = (double)newValue;
	}

	static void OnCornerRadiusPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var avatarView = (AvatarView)bindable;
		var corderRadius = (CornerRadius)newValue;

		avatarView.StrokeShape = new RoundRectangle
		{
			CornerRadius = corderRadius
		};
	}

	static void OnImageSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var avatarView = (AvatarView)bindable;
		avatarView.HandleImageChanged((ImageSource?)newValue);
	}

	static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var avatarView = (AvatarView)bindable;
		avatarView.avatarLabel.Text = (string)newValue;
	}

	void IImageSourcePart.UpdateIsLoading(bool isLoading)
	{
		if (!isLoading && wasImageLoading)
		{
			Handler?.UpdateValue(nameof(AvatarView));
		}

		wasImageLoading = isLoading;
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
			var offsetX = StrokeThickness + Padding.Left;
			var offsetY = StrokeThickness + Padding.Top;
#endif
			var imageWidth = Width - (StrokeThickness * 2) - Padding.Left - Padding.Right;
			var imageHeight = Height - (StrokeThickness * 2) - Padding.Top - Padding.Bottom;
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