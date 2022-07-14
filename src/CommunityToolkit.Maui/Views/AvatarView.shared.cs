namespace CommunityToolkit.Maui.Views;

using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;
using Font = Microsoft.Maui.Font;

/// <summary>Avatar content view.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this", Justification = "Is stupid")]
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

	/// <summary>Initializes a new instance of the <see cref="AvatarView"/> class.</summary>
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
		Content = AvatarLabel;
		InvalidateMeasure();
	}

	/// <summary>Gets or sets a value of the avatar border colour.</summary>
	public Color BorderColor
	{
		get => (Color)GetValue(BorderColorProperty);
		set => SetValue(BorderColorProperty, value);
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

	/// <summary>Gets or sets a value indicating whether avatar font auto scaling enabled property.</summary>
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

	/// <summary>Gets the avatar default text value.</summary>
	string IAvatarView.TextDefaultValue => (string)TextProperty.DefaultValue;

	/// <summary>Gets the corder radius property default value.</summary>
	CornerRadius IAvatarView.CornerRadiusDefaultValue => (CornerRadius)CornerRadiusProperty.DefaultValue;

	/// <summary>Gets a value indicating whether the avatar image source is empty.</summary>
	bool IImageSource.IsEmpty => AvatarImage.Source is null;

	/// <summary>Gets the image source.</summary>
	IImageSource IImageSourcePart.Source => ImageSource;

	/// <summary>Gets the image element aspect.</summary>
	Aspect Microsoft.Maui.IImage.Aspect => ((IImageElement)this).Aspect;

	/// <summary>Gets a value indicating whether the image element is opaque.</summary>
	bool Microsoft.Maui.IImage.IsOpaque => ((IImageElement)this).IsOpaque;

	/// <summary>Gets the avatar image aspect.</summary>
	Aspect IImageElement.Aspect => AvatarImage.Aspect;

	/// <summary>Gets the avatar image source.</summary>
	ImageSource IImageElement.Source => AvatarImage.Source;

	/// <summary>Gets a value indicating whether the avatar image is opaque.</summary>
	bool IImageElement.IsOpaque => AvatarImage.IsOpaque;

	/// <summary>Gets a value indicating whether the avatar image is loading.</summary>
	bool IImageElement.IsLoading => AvatarImage.IsLoading;

	/// <summary>Gets a value indicating whether the avatar image animation is playing.</summary>
	bool IImageElement.IsAnimationPlaying => AvatarImage.IsAnimationPlaying;

	/// <summary>Gets a value indicating whether the image element animation is playing.</summary>
	bool IImageSourcePart.IsAnimationPlaying => ((IImageElement)this).IsAnimationPlaying;

	/// <summary>Gets the avatar label text decorations.</summary>
	TextDecorations ILabel.TextDecorations => AvatarLabel.TextDecorations;

	/// <summary>Gets the horizontal text alignment.</summary>
	TextAlignment ITextAlignment.HorizontalTextAlignment => ((ITextAlignmentElement)this).HorizontalTextAlignment;

	/// <summary>Gets the vertical text alignment.</summary>
	TextAlignment ITextAlignment.VerticalTextAlignment => ((ITextAlignmentElement)this).VerticalTextAlignment;

	/// <summary>Gets the avatar label horizontal text alignment.</summary>
	TextAlignment ITextAlignmentElement.HorizontalTextAlignment => AvatarLabel.HorizontalTextAlignment;

	/// <summary>Gets the avatar label vertical text alignment.</summary>
	TextAlignment ITextAlignmentElement.VerticalTextAlignment => AvatarLabel.VerticalTextAlignment;

	/// <summary>Gets the line height.</summary>
	double ILabel.LineHeight => ((ILineHeightElement)this).LineHeight;

	/// <summary>Gets the avatar label line height.</summary>
	double ILineHeightElement.LineHeight => AvatarLabel.LineHeight;

	Image AvatarImage { get; } = new Image
	{
		Aspect = Aspect.AspectFill,
	};

	Label AvatarLabel { get; } = new Label
	{
		HorizontalTextAlignment = TextAlignment.Center,
		VerticalTextAlignment = TextAlignment.Center,
		Text = AvatarViewDefaults.DefaultText,
	};

	/// <summary>Gets the font size default value.</summary>
	/// <returns>Default font size for element.</returns>
	double IFontElement.FontSizeDefaultValueCreator()
	{
		return this.GetDefaultFontSize();
	}

	/// <summary>Gets a value indicating whether text property is set.</summary>
	/// <returns>True if set.</returns>
	bool IAvatarView.IsTextSet()
	{
		return IsSet(TextProperty);
	}

	/// <summary>Gets a value indicating whether border color property is set.</summary>
	/// <returns>True if set.</returns>
	bool IAvatarView.IsBorderColorSet()
	{
		return IsSet(BorderColorProperty);
	}

	/// <summary>Gets a value indicating whether border width property is set.</summary>
	/// <returns>True if set.</returns>
	bool IAvatarView.IsBorderWidthSet()
	{
		return IsSet(BorderWidthProperty);
	}

	/// <summary>Gets a value indicating whether corner radius property is set.</summary>
	/// <returns>True if set.</returns>
	bool IAvatarView.IsCornerRadiusSet()
	{
		return IsSet(CornerRadiusProperty);
	}

	/// <summary>Gets a value indicating whether image source property is set.</summary>
	/// <returns>True if set.</returns>
	bool IAvatarView.IsImageSourceSet()
	{
		return IsSet(ImageSourceProperty);
	}

	/// <summary>On border color property changed.</summary>
	/// <remarks>Apply as stroke to the element.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void IAvatarView.OnBorderColorPropertyChanged(Color oldValue, Color newValue)
	{
		Stroke = newValue;
	}

	/// <summary>On border width property changed.</summary>
	/// <remarks>Apply as stroke thickness to the element.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void IAvatarView.OnBorderWidthPropertyChanged(double oldValue, double newValue)
	{
		StrokeThickness = newValue;
	}

	/// <summary>On character spacing property changed.</summary>
	/// <remarks>Apply to avatar label.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void ITextElement.OnCharacterSpacingPropertyChanged(double oldValue, double newValue)
	{
		InvalidateMeasure();
		AvatarLabel.CharacterSpacing = newValue;
	}

	/// <summary>On corner radius property changed.</summary>
	/// <remarks>Apply corner radius as a round rectangle stroke shape to the element.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void IAvatarView.OnCornerRadiusPropertyChanged(CornerRadius oldValue, CornerRadius newValue)
	{
		StrokeShape = new RoundRectangle { CornerRadius = newValue };
	}

	/// <summary>On font attributes changed.</summary>
	/// <remarks>Handle font changed.  Apply to avatar label.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void IFontElement.OnFontAttributesChanged(FontAttributes oldValue, FontAttributes newValue)
	{
		HandleFontChanged();
		AvatarLabel.FontAttributes = newValue;
	}

	/// <summary>On font auto scaling enabled changed.</summary>
	/// <remarks>Handle font changed.  Apply to avatar label.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void IFontElement.OnFontAutoScalingEnabledChanged(bool oldValue, bool newValue)
	{
		HandleFontChanged();
		AvatarLabel.FontAutoScalingEnabled = newValue;
	}

	/// <summary>On font family changed.</summary>
	/// <remarks>Handle font changed. Apply to avatar label.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void IFontElement.OnFontFamilyChanged(string oldValue, string newValue)
	{
		HandleFontChanged();
		AvatarLabel.FontFamily = newValue;
	}

	/// <summary>On font size changed.</summary>
	/// <remarks>Handle font changed.  Apply to avatar label.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void IFontElement.OnFontSizeChanged(double oldValue, double newValue)
	{
		HandleFontChanged();
		AvatarLabel.FontSize = newValue;
	}

	/// <summary>On image source changed.</summary>
	/// <remarks>Handle image changed.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void IAvatarView.OnImageSourceChanged(object oldValue, object newValue)
	{
		HandleImageChanged((ImageSource)newValue);
	}

	/// <summary>On text color property changed.</summary>
	/// <remarks>Apply to avatar label.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void ITextElement.OnTextColorPropertyChanged(Color oldValue, Color newValue)
	{
		AvatarLabel.TextColor = newValue;
	}

	/// <summary>On text property changed.</summary>
	/// <remarks>Apply to avatar label.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void IAvatarView.OnTextPropertyChanged(string oldValue, string newValue)
	{
		AvatarLabel.Text = newValue;
	}

	/// <summary>On text transform changed.</summary>
	/// <remarks> Invalidate measure, apply to avatar label.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void ITextElement.OnTextTransformChanged(TextTransform oldValue, TextTransform newValue)
	{
		InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
		AvatarLabel.TextTransform = newValue;
	}

	/// <summary>Padding default value creator.</summary>
	/// <returns>Default padding Thickness.</returns>
	Thickness IAvatarView.PaddingDefaultValueCreator()
	{
		return default;
	}

	/// <inheritdoc/>
	string ITextElement.UpdateFormsText(string original, TextTransform transform)
	{
		return TextTransformUtilites.GetTransformedText(original, transform);
	}

	/// <summary>Update is loading.</summary>
	/// <param name="isLoading">True if updating.</param>
	void IImageSourcePart.UpdateIsLoading(bool isLoading)
	{
		if (!isLoading && wasImageLoading)
		{
			Handler?.UpdateValue(nameof(AvatarView));
		}

		wasImageLoading = isLoading;
	}

	/// <summary>Raise image source property changed.</summary>
	/// <remarks>Raise on avatar image.</remarks>
	void IImageElement.RaiseImageSourcePropertyChanged()
	{
		((IImageElement)AvatarImage).RaiseImageSourcePropertyChanged();
	}

	/// <inheritdoc/>
	void IImageElement.OnImageSourceSourceChanged(object sender, EventArgs e)
	{
		((IImageElement)AvatarImage).OnImageSourceSourceChanged(sender, e);
	}

	/// <summary>On horizontal text alignment property changed.</summary>
	/// <remarks>Apply to avatar label.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void ITextAlignmentElement.OnHorizontalTextAlignmentPropertyChanged(TextAlignment oldValue, TextAlignment newValue)
	{
		((ITextAlignmentElement)AvatarLabel).OnHorizontalTextAlignmentPropertyChanged(oldValue, newValue);
	}

	/// <summary>On line height changed.</summary>
	/// <remarks>Apply to avatar label.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void ILineHeightElement.OnLineHeightChanged(double oldValue, double newValue)
	{
		((ILineHeightElement)AvatarLabel).OnLineHeightChanged(oldValue, newValue);
	}

	static void OnBorderColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarView)bindable).OnBorderColorPropertyChanged((Color)oldValue, (Color)newValue);
	}

	static void OnBorderWidthPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarView)bindable).OnBorderWidthPropertyChanged((double)oldValue, (double)newValue);
	}

	static void OnCornerRadiusPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarView)bindable).OnCornerRadiusPropertyChanged((CornerRadius)oldValue, (CornerRadius)newValue);
	}

	static void OnImageSourceChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarView)bindable).OnImageSourceChanged((ImageSource)oldValue, (ImageSource)newValue);
	}

	static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarView)bindable).OnTextPropertyChanged((string)oldValue, (string)newValue);
	}

	void HandleCornerRadiusChanged()
	{
		CornerRadius cornerRadius = new ();
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
		AvatarImage.Source = newValue;
		Content = newValue is not null ? AvatarImage : AvatarLabel;
		HandleCornerRadiusChanged();
	}
}