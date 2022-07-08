using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;
using Font = Microsoft.Maui.Font;

namespace CommunityToolkit.Maui.Views;

/// <summary>Avatar content view.</summary>
public class AvatarView : Border, IAvatarView, IFontElement, ITextElement, IImageElement, ITextAlignmentElement, ILineHeightElement, ICornerElement
{
	/// <summary>The backing store for the <see cref="BorderColor" /> bindable property.</summary>
	public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(nameof(IAvatarView.BorderColor), typeof(Color), typeof(IAvatarView), defaultValue: AvatarViewDefaults.DefaultBorderColor, propertyChanged: OnBorderColorPropertyChanged);

	/// <summary>The backing store for the <see cref="BorderWidth" /> bindable property.</summary>
	public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(nameof(IAvatarView.BorderWidth), typeof(double), typeof(IAvatarView), defaultValue: AvatarViewDefaults.DefaultBorderWidth, propertyChanged: OnBorderWidthPropertyChanged);

	/// <summary>The backing store for the <see cref="CornerRadius" /> bindable property.</summary>
	public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(nameof(IAvatarView.CornerRadius), typeof(CornerRadius), typeof(ICornerElement), defaultValue: AvatarViewDefaults.DefaultCornerRadius, propertyChanged: OnCornerRadiusPropertyChanged);

	/// <summary>The backing store for the <see cref="IFontElement.FontAttributes" /> bindable property.</summary>
	public static readonly BindableProperty FontAttributesProperty = FontElement.FontAttributesProperty;

	/// <summary>The backing store for the <see cref="IFontElement.FontAutoScalingEnabled" /> bindable property.</summary>
	public static readonly BindableProperty FontAutoScalingEnabledProperty = FontElement.FontAutoScalingEnabledProperty;

	/// <summary>The backing store for the <see cref="IFontElement.FontFamily" /> bindable property.</summary>
	public static readonly BindableProperty FontFamilyProperty = FontElement.FontFamilyProperty;

	/// <summary>The backing store for the <see cref="IFontElement.FontSize" /> bindable property.</summary>
	public static readonly BindableProperty FontSizeProperty = FontElement.FontSizeProperty;

	/// <summary>The backing store for the <see cref="ImageSource" /> bindable property.</summary>
	public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(nameof(ImageSource), typeof(ImageSource), typeof(IImageElement), default(ImageSource), propertyChanged: OnImageSourceChanged);

	/// <summary>The backing store for the <see cref="ITextStyle.TextColor" /> bindable property.</summary>
	public static readonly BindableProperty TextColorProperty = TextElement.TextColorProperty;

	/// <summary>The backing store for the <see cref="Text" /> bindable property.</summary>
	public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(AvatarView), defaultValue: AvatarViewDefaults.DefaultText, propertyChanged: OnTextPropertyChanged);

	/// <summary>The backing store for the <see cref="TextTransform" /> bindable property.</summary>
	public static readonly BindableProperty TextTransformProperty = TextElement.TextTransformProperty;

	bool wasImageLoading;

	/// <summary>Initialises a new instance of the <see cref="AvatarView"/> class.</summary>
	public AvatarView()
	{
		IsEnabled = true;

		HorizontalOptions = VerticalOptions = LayoutOptions.Center;
		HeightRequest = AvatarViewDefaults.DefaultHeightRequest;
		WidthRequest = AvatarViewDefaults.DefaultWidthRequest;
		Stroke = AvatarViewDefaults.DefaultBorderColor;
		StrokeThickness = AvatarViewDefaults.DefaultBorderWidth;
		StrokeShape = new RoundRectangle
		{
			CornerRadius = new CornerRadius(AvatarViewDefaults.DefaultCornerRadius.TopLeft, AvatarViewDefaults.DefaultCornerRadius.TopRight, AvatarViewDefaults.DefaultCornerRadius.BottomLeft, AvatarViewDefaults.DefaultCornerRadius.BottomRight),
		};

		Content = avatarLabel;

		InvalidateMeasure();
	}

	/// <summary>Gets or sets the avatar padding property.</summary>
	public Thickness AvatarPadding
	{
		get => (Thickness)GetValue(PaddingElement.PaddingProperty);
		set => SetValue(PaddingElement.PaddingProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar border colour.</summary>
	public Color BorderColor
	{
		get => (Color)GetValue(BorderWidthProperty);
		set => SetValue(BorderWidthProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar border width.</summary>
	public double BorderWidth
	{
		get => (double)GetValue(BorderWidthProperty);
		set => SetValue(BorderWidthProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar text character spacing property.</summary>
	public double CharacterSpacing
	{
		get => (double)GetValue(TextElement.CharacterSpacingProperty);
		set => SetValue(TextElement.CharacterSpacingProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar corder radius property.</summary>
	public CornerRadius CornerRadius
	{
		get => (CornerRadius)GetValue(CornerRadiusProperty);
		set => SetValue(CornerRadiusProperty, value);
	}

	/// <summary>Gets or sets the avatar font.</summary>
	public Font Font { get; set; } = Font.SystemFontOfSize(AvatarViewDefaults.FontSize);

	/// <summary>Gets or sets a value of the avatar font attributes property.</summary>
	public FontAttributes FontAttributes
	{
		get => (FontAttributes)GetValue(FontElement.FontAttributesProperty);
		set => SetValue(FontElement.FontAttributesProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar font auto scaling enabled property.</summary>
	public bool FontAutoScalingEnabled
	{
		get => (bool)GetValue(FontElement.FontAutoScalingEnabledProperty);
		set => SetValue(FontElement.FontAutoScalingEnabledProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar font family property.</summary>
	public string FontFamily
	{
		get => (string)GetValue(FontElement.FontFamilyProperty);
		set => SetValue(FontElement.FontFamilyProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar font size property.</summary>
	[TypeConverter(typeof(FontSizeConverter))]
	public double FontSize
	{
		get => (double)GetValue(FontElement.FontSizeProperty);
		set => SetValue(FontElement.FontSizeProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar image source property.</summary>
	[TypeConverter(typeof(ImageSourceConverter))]
	public ImageSource ImageSource
	{
		get => (ImageSource)GetValue(ImageElement.ImageSourceProperty);
		set => SetValue(ImageElement.ImageSourceProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar text property.</summary>
	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar text colour property.</summary>
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

	Image avatarImage { get; } = new Image
	{
		Aspect = Aspect.AspectFill,
	};

	Label avatarLabel { get; } = new Label
	{
		HorizontalTextAlignment = TextAlignment.Center,
		VerticalTextAlignment = TextAlignment.Center,
		Text = AvatarViewDefaults.DefaultText
	};

	string IAvatarView.TextDefaultValue => (string)TextProperty.DefaultValue;

	CornerRadius IAvatarView.CornerRadiusDefaultValue => (CornerRadius)CornerRadiusProperty.DefaultValue;

	bool IImageSource.IsEmpty => avatarImage.Source is null;

	IImageSource IImageSourcePart.Source => ImageSource;

	Aspect Microsoft.Maui.IImage.Aspect => ((IImageElement)this).Aspect;

	bool Microsoft.Maui.IImage.IsOpaque => ((IImageElement)this).IsOpaque;

	Aspect IImageElement.Aspect => avatarImage.Aspect;

	ImageSource IImageElement.Source => avatarImage.Source;

	bool IImageElement.IsOpaque => avatarImage.IsOpaque;

	bool IImageElement.IsLoading => avatarImage.IsLoading;

	bool IImageElement.IsAnimationPlaying => avatarImage.IsAnimationPlaying;

	bool IImageSourcePart.IsAnimationPlaying => ((IImageElement)this).IsAnimationPlaying;

	TextDecorations ILabel.TextDecorations => avatarLabel.TextDecorations;

	TextAlignment ITextAlignment.HorizontalTextAlignment => ((ITextAlignmentElement)this).HorizontalTextAlignment;

	TextAlignment ITextAlignment.VerticalTextAlignment => ((ITextAlignmentElement)this).VerticalTextAlignment;

	TextAlignment ITextAlignmentElement.HorizontalTextAlignment => avatarLabel.HorizontalTextAlignment;

	TextAlignment ITextAlignmentElement.VerticalTextAlignment => avatarLabel.VerticalTextAlignment;

	double ILabel.LineHeight => ((ILineHeightElement)this).LineHeight;

	double ILineHeightElement.LineHeight => avatarLabel.LineHeight;

	static void OnBorderColorPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
																							((IAvatarView)bindable).OnBorderColorPropertyChanged((Color)oldValue, (Color)newValue);

	static void OnBorderWidthPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
		((IAvatarView)bindable).OnBorderWidthPropertyChanged((double)oldValue, (double)newValue);

	static void OnCornerRadiusPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
		((IAvatarView)bindable).OnCornerRadiusPropertyChanged((CornerRadius)oldValue, (CornerRadius)newValue);

	static void OnImageSourceChanged(BindableObject bindable, object oldValue, object newValue) =>
		((IAvatarView)bindable).OnImageSourceChanged((ImageSource)oldValue, (ImageSource)newValue);

	static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
		((IAvatarView)bindable).OnTextPropertyChanged((string)oldValue, (string)newValue);

	void HandleCornerRadiusChanged()
	{
		CornerRadius cornerRadius = new();
		if (CornerRadius != cornerRadius)
		{
			StrokeShape = new RoundRectangle { CornerRadius = cornerRadius };
		}

		StrokeShape = new RoundRectangle { CornerRadius = CornerRadius };
	}

	void HandleFontChanged()
	{
		Handler?.UpdateValue(nameof(ITextStyle.Font));
		InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
	}

	void HandleImageChanged(ImageSource newValue)
	{
		avatarImage.Source = newValue;
		Content = newValue is not null ? avatarImage : avatarLabel;
		HandleCornerRadiusChanged();
	}

	double IFontElement.FontSizeDefaultValueCreator() => this.GetDefaultFontSize();

	bool IAvatarView.IsTextSet() => IsSet(TextProperty);

	bool IAvatarView.IsBorderColorSet() => IsSet(BorderColorProperty);

	bool IAvatarView.IsBorderWidthSet() => IsSet(BorderWidthProperty);

	bool IAvatarView.IsCornerRadiusSet() => IsSet(CornerRadiusProperty);

	bool IAvatarView.IsImageSourceSet() => IsSet(ImageSourceProperty);

	void IAvatarView.OnBorderColorPropertyChanged(Color oldValue, Color newValue) => Stroke = newValue;

	void IAvatarView.OnBorderWidthPropertyChanged(double oldValue, double newValue) => StrokeThickness = newValue;

	void ITextElement.OnCharacterSpacingPropertyChanged(double oldValue, double newValue)
	{
		InvalidateMeasure();
		avatarLabel.CharacterSpacing = newValue;
	}

	void IAvatarView.OnCornerRadiusPropertyChanged(CornerRadius oldValue, CornerRadius newValue)
	{
		StrokeShape = new RoundRectangle { CornerRadius = newValue };
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

	void IAvatarView.OnImageSourceChanged(object oldValue, object newValue) => HandleImageChanged((ImageSource)newValue);

	void ITextElement.OnTextColorPropertyChanged(Color oldValue, Color newValue) => avatarLabel.TextColor = newValue;

	void IAvatarView.OnTextPropertyChanged(string oldValue, string newValue) => avatarLabel.Text = newValue;

	void ITextElement.OnTextTransformChanged(TextTransform oldValue, TextTransform newValue)
	{
		InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
		avatarLabel.TextTransform = newValue;
	}

	Thickness IAvatarView.PaddingDefaultValueCreator() => default;

	string ITextElement.UpdateFormsText(string original, TextTransform transform) =>
		TextTransformUtilites.GetTransformedText(original, transform);

	void IImageSourcePart.UpdateIsLoading(bool isLoading)
	{
		if (!isLoading && wasImageLoading)
		{
			Handler?.UpdateValue(nameof(AvatarView));
		}

		wasImageLoading = isLoading;
	}

	void IImageElement.RaiseImageSourcePropertyChanged() =>
		((IImageElement)avatarImage).RaiseImageSourcePropertyChanged();

	void IImageElement.OnImageSourceSourceChanged(object sender, EventArgs e) =>
		((IImageElement)avatarImage).OnImageSourceSourceChanged(sender, e);

	void ITextAlignmentElement.OnHorizontalTextAlignmentPropertyChanged(TextAlignment oldValue, TextAlignment newValue) =>
		((ITextAlignmentElement)avatarLabel).OnHorizontalTextAlignmentPropertyChanged(oldValue, newValue);

	void ILineHeightElement.OnLineHeightChanged(double oldValue, double newValue) =>
		((ILineHeightElement)avatarLabel).OnLineHeightChanged(oldValue, newValue);
}