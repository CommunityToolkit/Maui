using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views;

/// <summary>AvatarView control.</summary>
public class AvatarView : Border, IAvatarView, IBorderElement, IFontElement, ITextElement, IImageElement, ITextAlignmentElement, ILineHeightElement, ICornerElement
{
	/// <summary>The backing store for the <see cref="BorderColor" /> bindable property.</summary>
	public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(nameof(BorderColor), typeof(Color), typeof(IAvatarView), defaultValue: AvatarViewDefaults.DefaultBorderColor, propertyChanged: OnBorderColorPropertyChanged);

	/// <summary>The backing store for the <see cref="BorderWidth" /> bindable property.</summary>
	public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(nameof(BorderWidth), typeof(double), typeof(IAvatarView), defaultValue: AvatarViewDefaults.DefaultBorderWidth, propertyChanged: OnBorderWidthPropertyChanged);

	/// <summary>The backing store for the <see cref="CornerRadius" /> bindable property.</summary>
	public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(nameof(CornerRadius), typeof(CornerRadius), typeof(ICornerElement), defaultValue: AvatarViewDefaults.DefaultCornerRadius, propertyChanged: OnCornerRadiusPropertyChanged);

	/// <summary>The backing store for the <see cref="IFontElement.FontAttributes" /> bindable property.</summary>
	public static readonly BindableProperty FontAttributesProperty = FontElement.FontAttributesProperty;

	/// <summary>The backing store for the <see cref="IFontElement.FontAutoScalingEnabled" /> bindable property.</summary>
	public static readonly BindableProperty FontAutoScalingEnabledProperty = FontElement.FontAutoScalingEnabledProperty;

	/// <summary>The backing store for the <see cref="IFontElement.FontFamily" /> bindable property.</summary>
	public static readonly BindableProperty FontFamilyProperty = FontElement.FontFamilyProperty;

	/// <summary>The backing store for the <see cref="IFontElement.FontSize" /> bindable property.</summary>
	public static readonly BindableProperty FontSizeProperty = FontElement.FontSizeProperty;

	/// <summary>The backing store for the <see cref="ImageSource" /> bindable property.</summary>
	public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(nameof(ImageSource), typeof(ImageSource), typeof(IImageElement), default(ImageSource), propertyChanged: OnImageSourcePropertyChanged);

	/// <summary>The backing store for the <see cref="ITextStyle.TextColor" /> bindable property.</summary>
	public static readonly BindableProperty TextColorProperty = TextElement.TextColorProperty;

	/// <summary>The backing store for the <see cref="Text" /> bindable property.</summary>
	public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(AvatarView), defaultValue: AvatarViewDefaults.DefaultText, propertyChanged: OnTextPropertyChanged);

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
		Text = AvatarViewDefaults.DefaultText,
	};

	bool wasImageLoading;

	/// <summary>Initializes a new instance of the <see cref="AvatarView"/> class.</summary>
	public AvatarView()
	{
		PropertyChanged += HandlePropertyChanged;

		IsEnabled = true;
		HorizontalOptions = VerticalOptions = LayoutOptions.Center;
		HeightRequest = AvatarViewDefaults.DefaultHeightRequest;
		WidthRequest = AvatarViewDefaults.DefaultWidthRequest;
		Padding = AvatarViewDefaults.DefaultPadding;
		Stroke = AvatarViewDefaults.DefaultBorderColor;
		StrokeThickness = AvatarViewDefaults.DefaultBorderWidth;
		StrokeShape = new RoundRectangle
		{
			CornerRadius = new CornerRadius(AvatarViewDefaults.DefaultCornerRadius.TopLeft, AvatarViewDefaults.DefaultCornerRadius.TopRight, AvatarViewDefaults.DefaultCornerRadius.BottomLeft, AvatarViewDefaults.DefaultCornerRadius.BottomRight),
		};
		Content = avatarLabel;
		avatarImage.SetBinding(WidthRequestProperty, new Binding(nameof(WidthRequest), source: this));
		avatarImage.SetBinding(HeightRequestProperty, new Binding(nameof(HeightRequest), source: this));
	}

	/// <summary>Gets or sets the control font.</summary>
	public Microsoft.Maui.Font Font { get; set; } = Microsoft.Maui.Font.SystemFontOfSize((double)FontElement.FontSizeProperty.DefaultValue);

	/// <summary>Gets or sets a value of the control border colour.</summary>
	public Color BorderColor
	{
		get => (Color)GetValue(BorderColorProperty);
		set => SetValue(BorderColorProperty, value);
	}

	/// <summary>Gets or sets a value of the control border width.</summary>
	public double BorderWidth
	{
		get => (double)GetValue(BorderWidthProperty);
		set => SetValue(BorderWidthProperty, value);
	}

	/// <summary>Gets or sets a value of the control text character spacing property.</summary>
	public double CharacterSpacing
	{
		get => (double)GetValue(TextElement.CharacterSpacingProperty);
		set => SetValue(TextElement.CharacterSpacingProperty, value);
	}

	/// <summary>Gets or sets a value of the control corner radius property.</summary>
	public CornerRadius CornerRadius
	{
		get => (CornerRadius)GetValue(CornerRadiusProperty);
		set => SetValue(CornerRadiusProperty, value);
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

	/// <summary>Gets or sets a value of the control image source property.</summary>
	[TypeConverter(typeof(ImageSourceConverter))]
	public ImageSource ImageSource
	{
		get => (ImageSource)GetValue(ImageSourceProperty);
		set => SetValue(ImageSourceProperty, value);
	}

	/// <summary>Gets or sets a value of the control text property.</summary>
	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
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

	Aspect Microsoft.Maui.IImage.Aspect => ((IImageElement)this).Aspect;

	Aspect IImageElement.Aspect => avatarImage.Aspect;

	Color IBorderElement.BorderColorDefaultValue => (Color)BorderColorProperty.DefaultValue;

	double IBorderElement.BorderWidthDefaultValue => (double)BorderWidthProperty.DefaultValue;

	int IBorderElement.CornerRadius => (int)GetAverageCorderRadius(CornerRadius);

	int IBorderElement.CornerRadiusDefaultValue => (int)GetAverageCorderRadius((CornerRadius)CornerRadiusProperty.DefaultValue);

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

	IImageSource IImageSourcePart.Source => ImageSource;

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

	string ITextElement.UpdateFormsText(string original, TextTransform transform) => TextTransformUtilites.GetTransformedText(original, transform);

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