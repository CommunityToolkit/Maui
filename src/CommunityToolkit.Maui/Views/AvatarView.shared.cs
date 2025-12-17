using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;
using ILineHeightElement = Microsoft.Maui.Controls.ILineHeightElement;

namespace CommunityToolkit.Maui.Views;

/// <summary>AvatarView control.</summary>
public partial class AvatarView : Border, IAvatarView, IBorderElement, IFontElement, ITextElement, IImageElement, ITextAlignmentElement, ILineHeightElement, ICornerElement
{
	/// <summary>The backing store for the <see cref="IFontElement.FontAttributes" /> bindable property.</summary>
	public static readonly BindableProperty FontAttributesProperty = FontElement.FontAttributesProperty;

	/// <summary>The backing store for the <see cref="IFontElement.FontAutoScalingEnabled" /> bindable property.</summary>
	public static readonly BindableProperty FontAutoScalingEnabledProperty = FontElement.FontAutoScalingEnabledProperty;

	/// <summary>The backing store for the <see cref="IFontElement.FontFamily" /> bindable property.</summary>
	public static readonly BindableProperty FontFamilyProperty = FontElement.FontFamilyProperty;

	/// <summary>The backing store for the <see cref="IFontElement.FontSize" /> bindable property.</summary>
	public static readonly BindableProperty FontSizeProperty = FontElement.FontSizeProperty;

	/// <summary>The backing store for the <see cref="ITextStyle.TextColor" /> bindable property.</summary>
	public static readonly BindableProperty TextColorProperty = TextElement.TextColorProperty;

	/// <summary>The backing store for the <see cref="TextTransform" /> bindable property.</summary>
	public static readonly BindableProperty TextTransformProperty = TextElement.TextTransformProperty;

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

	/// <summary>Initializes a new instance of the <see cref="AvatarView"/> class.</summary>
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

	/// <summary>Gets or sets the control font.</summary>
	public Microsoft.Maui.Font Font { get; set; } = Microsoft.Maui.Font.SystemFontOfSize((double)FontElement.FontSizeProperty.DefaultValue);

	/// <summary>Gets or sets a value of the control text character spacing property.</summary>
	public double CharacterSpacing
	{
		get => (double)GetValue(TextElement.CharacterSpacingProperty);
		set => SetValue(TextElement.CharacterSpacingProperty, value);
	}

	/// <summary>Gets or sets a value of the control font attributes property.</summary>
	public FontAttributes FontAttributes
	{
		get => (FontAttributes)GetValue(FontElement.FontAttributesProperty);
		set => SetValue(FontElement.FontAttributesProperty, value);
	}

	/// <summary>Gets or sets a value indicating whether control font auto scaling enabled property.</summary>
	public bool FontAutoScalingEnabled
	{
		get => (bool)GetValue(FontElement.FontAutoScalingEnabledProperty);
		set => SetValue(FontElement.FontAutoScalingEnabledProperty, value);
	}

	/// <summary>Gets or sets a value of the control font family property.</summary>
	public string FontFamily
	{
		get => (string)GetValue(FontElement.FontFamilyProperty);
		set => SetValue(FontElement.FontFamilyProperty, value);
	}

	/// <summary>Gets or sets a value of the control font size property.</summary>
	[TypeConverter(typeof(FontSizeConverter))]
	public double FontSize
	{
		get => (double)GetValue(FontElement.FontSizeProperty);
		set => SetValue(FontElement.FontSizeProperty, value);
	}

	/// <summary>Gets or sets a value of the control text colour property.</summary>
	public Color TextColor
	{
		get => (Color)GetValue(TextElement.TextColorProperty);
		set => SetValue(TextElement.TextColorProperty, value);
	}

	/// <inheritdoc/>
	public TextTransform TextTransform
	{
		get => (TextTransform)GetValue(TextElement.TextTransformProperty);
		set => SetValue(TextElement.TextTransformProperty, value);
	}

	/// <summary>Gets or sets a value of the control border colour.</summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnBorderColorPropertyChanged))]
	public partial Color BorderColor { get; set; } = AvatarViewDefaults.BorderColor;

	/// <summary>Gets or sets a value of the control border width.</summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnBorderWidthPropertyChanged))]
	public partial double BorderWidth { get; set; } = AvatarViewDefaults.BorderWidth;

	/// <summary>Gets or sets a value of the control corner radius property.</summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnCornerRadiusPropertyChanged))]
	public partial CornerRadius CornerRadius { get; set; } = AvatarViewDefaults.CornerRadius;

	/// <summary>Gets or sets a value of the control image source property.</summary>
	[TypeConverter(typeof(ImageSourceConverter))]
	[BindableProperty(PropertyChangedMethodName = nameof(OnImageSourcePropertyChanged))]
	public partial ImageSource? ImageSource { get; set; }

	/// <summary>Gets or sets a value of the control text property.</summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnTextPropertyChanged))]
	public partial string Text { get; set; } = AvatarViewDefaults.Text;

	Aspect Microsoft.Maui.IImage.Aspect => ((IImageElement)this).Aspect;

	Aspect IImageElement.Aspect => avatarImage.Aspect;

	Color IBorderElement.BorderColorDefaultValue => AvatarViewDefaults.BorderColor;

	double IBorderElement.BorderWidthDefaultValue => AvatarViewDefaults.BorderWidth;

	int IBorderElement.CornerRadius => (int)GetAverageCorderRadius(CornerRadius);

	int IBorderElement.CornerRadiusDefaultValue => (int)GetAverageCorderRadius(AvatarViewDefaults.CornerRadius);

	TextAlignment ITextAlignment.HorizontalTextAlignment => ((ITextAlignmentElement)this).HorizontalTextAlignment;

	TextAlignment ITextAlignmentElement.HorizontalTextAlignment => avatarLabel.HorizontalTextAlignment;

	bool IImageElement.IsAnimationPlaying => avatarImage.IsAnimationPlaying;

	bool IImageSourcePart.IsAnimationPlaying => ((IImageElement)this).IsAnimationPlaying;

	bool IImageSource.IsEmpty => avatarImage.Source is null;

	bool IImageElement.IsLoading => avatarImage.IsLoading;

	bool Microsoft.Maui.IImage.IsOpaque => ((IImageElement)this).IsOpaque;

	bool IImageElement.IsOpaque => avatarImage.IsOpaque;

	double ILabel.LineHeight => ((ILineHeightElement)this).LineHeight;

	double ILineHeightElement.LineHeight => avatarLabel.LineHeight;

	IImageSource? IImageSourcePart.Source => ImageSource;

	ImageSource IImageElement.Source => avatarImage.Source;

	TextAlignment ITextAlignment.VerticalTextAlignment => ((ITextAlignmentElement)this).VerticalTextAlignment;

	TextAlignment ITextAlignmentElement.VerticalTextAlignment => avatarLabel.VerticalTextAlignment;

	TextDecorations ILabel.TextDecorations => avatarLabel.TextDecorations;

	double IFontElement.FontSizeDefaultValueCreator() => this.GetDefaultFontSize();

	bool IBorderElement.IsBackgroundColorSet() => IsSet(BackgroundColorProperty);

	bool IBorderElement.IsBackgroundSet() => IsSet(BackgroundProperty);

	bool IBorderElement.IsBorderColorSet() => IsSet(BorderColorProperty);

	bool IBorderElement.IsBorderWidthSet() => IsSet(BorderWidthProperty);

	bool IBorderElement.IsCornerRadiusSet() => IsSet(CornerRadiusProperty);

	void IBorderElement.OnBorderColorPropertyChanged(Color oldValue, Color newValue) => Stroke = newValue;

	void ITextElement.OnCharacterSpacingPropertyChanged(double oldValue, double newValue)
	{
		InvalidateMeasure();
		avatarLabel.CharacterSpacing = newValue;
	}

	void IFontElement.OnFontAttributesChanged(FontAttributes oldValue, FontAttributes newValue)
	{
		HandleFontChanged();
		avatarLabel.FontAttributes = newValue;
	}

	void IFontElement.OnFontAutoScalingEnabledChanged(bool oldValue, bool newValue)
	{
		HandleFontChanged();
		avatarLabel.FontAutoScalingEnabled = newValue;
	}

	void IFontElement.OnFontFamilyChanged(string oldValue, string newValue)
	{
		HandleFontChanged();
		avatarLabel.FontFamily = newValue;
	}

	void IFontElement.OnFontSizeChanged(double oldValue, double newValue)
	{
		HandleFontChanged();
		avatarLabel.FontSize = newValue;
	}

	void ITextAlignmentElement.OnHorizontalTextAlignmentPropertyChanged(TextAlignment oldValue, TextAlignment newValue) =>
		((ITextAlignmentElement)avatarLabel).OnHorizontalTextAlignmentPropertyChanged(oldValue, newValue);

	void IImageElement.OnImageSourceSourceChanged(object sender, EventArgs e) =>
		((IImageElement)avatarImage).OnImageSourceSourceChanged(sender, e);

	void ILineHeightElement.OnLineHeightChanged(double oldValue, double newValue) =>
		((ILineHeightElement)avatarLabel).OnLineHeightChanged(oldValue, newValue);

	void ITextElement.OnTextColorPropertyChanged(Color oldValue, Color newValue) => avatarLabel.TextColor = newValue;

	void ITextElement.OnTextTransformChanged(TextTransform oldValue, TextTransform newValue)
	{
		InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
		avatarLabel.TextTransform = newValue;
	}

	void IImageElement.RaiseImageSourcePropertyChanged() => ((IImageElement)avatarImage).RaiseImageSourcePropertyChanged();

	string ITextElement.UpdateFormsText(string original, TextTransform transform) => TextTransformUtilities.GetTransformedText(original, transform);

	void IImageSourcePart.UpdateIsLoading(bool isLoading)
	{
		if (!isLoading && wasImageLoading)
		{
			Handler?.UpdateValue(nameof(AvatarView));
		}

		wasImageLoading = isLoading;
	}

	static double GetAverageCorderRadius(in CornerRadius cornerRadius) =>
		new[] { cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomLeft, cornerRadius.BottomRight }.Average();

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
		Handler?.UpdateValue(nameof(ITextStyle.Font));
		InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
	}

	void HandleImageChanged(ImageSource? newValue)
	{
		avatarImage.Source = newValue;
		if (newValue is not null)
		{
			Content = avatarImage;
		}
		else
		{
			Content = avatarLabel;
		}
	}

	void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		// Ensure avatarImage is clipped to the bounds of the AvatarView whenever its Height, Width, CornerRadius and Padding properties change
		if ((e.PropertyName == HeightProperty.PropertyName
			 || e.PropertyName == WidthProperty.PropertyName
			 || e.PropertyName == PaddingProperty.PropertyName
			 || e.PropertyName == ImageSourceProperty.PropertyName
			 || e.PropertyName == BorderWidthProperty.PropertyName
			 || e.PropertyName == CornerRadiusProperty.PropertyName
			 || e.PropertyName == StrokeThicknessProperty.PropertyName)
			&& Height >= 0 // The default value of Height (before the view is drawn onto the page) is -1
			&& Width >= 0 // The default value of Y (before the view is drawn onto the page) is -1
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