using System.ComponentModel;
using Microsoft.Maui.Controls.Internals;

namespace CommunityToolkit.Maui.Controls;

/// <summary>Avatar box view.</summary>
public partial class AvatarViewBorder : Border, ITextElement, IFontElement, IImageElement
{
	/// <summary>The backing store for the <see cref="Text" /> bindable property.</summary>
	public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(AvatarViewBorder), defaultValue: "?", propertyChanged: (bindable, _, newValue) => OnTextChanged(bindable, newValue));

	static void OnTextChanged(BindableObject bindable, object newValue)
	{
		((AvatarViewBorder)bindable).InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
		((AvatarViewBorder)bindable).HandleTextChanged(bindable, (string)newValue);
	}

	void HandleTextChanged(BindableObject bindable, string newValue)
	{
		if (bindable is null)
		{
			return;
		}

		label.Text = newValue;
	}

	/// <summary>Initialises a new instance of the Avatar Box View class.</summary>
	public AvatarViewBorder()
	{
		Content = label;
	}

	/// <summary>Avatar Label.</summary>
	readonly Label label = new()
	{
		Text = "?",
		IsVisible = true,
		VerticalOptions = LayoutOptions.Center,
		HorizontalOptions = LayoutOptions.Center,
		HorizontalTextAlignment = TextAlignment.Center,
		VerticalTextAlignment = TextAlignment.Center,
		Padding = 0,
		Margin = 0,
		WidthRequest = -1,
		HeightRequest = -1,
	};

	/// <inheritdoc/>
	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	/// <inheritdoc/>
	public Color TextColor
	{
		get => (Color)GetValue(TextElement.TextColorProperty);
		set => SetValue(TextElement.TextColorProperty, value);
	}

	/// <inheritdoc/>
	public double CharacterSpacing
	{
		get => (double)GetValue(TextElement.CharacterSpacingProperty);
		set => SetValue(TextElement.CharacterSpacingProperty, value);
	}

	/// <inheritdoc/>
	public TextTransform TextTransform
	{
		get => (TextTransform)GetValue(TextElement.TextTransformProperty);
		set => SetValue(TextElement.TextTransformProperty, value);
	}

	/// <inheritdoc/>
	public FontAttributes FontAttributes
	{
		get => (FontAttributes)GetValue(FontElement.FontAttributesProperty);
		set => SetValue(FontElement.FontAttributesProperty, value);
	}

	/// <inheritdoc/>
	public string FontFamily
	{
		get => (string)GetValue(FontElement.FontFamilyProperty);
		set => SetValue(FontElement.FontFamilyProperty, value);
	}

	/// <inheritdoc/>
	[TypeConverter(typeof(FontSizeConverter))]
	public double FontSize
	{
		get => (double)GetValue(FontElement.FontSizeProperty);
		set => SetValue(FontElement.FontSizeProperty, value);
	}

	/// <inheritdoc/>
	public bool FontAutoScalingEnabled
	{
		get => (bool)GetValue(FontElement.FontAutoScalingEnabledProperty);
		set => SetValue(FontElement.FontAutoScalingEnabledProperty, value);
	}

	/// <inheritdoc/>
	public ImageSource ImageSource
	{
		get => (ImageSource)GetValue(ImageElement.ImageSourceProperty);
		set => SetValue(ImageElement.ImageSourceProperty, value);
	}

	/// <inheritdoc/>
	protected override void OnBindingContextChanged()
	{
		ImageSource image = ImageSource;
		if (image != null)
		{
			SetInheritedBindingContext(image, BindingContext);
		}

		base.OnBindingContextChanged();
	}

	Aspect IImageElement.Aspect => Aspect.AspectFit;
	ImageSource IImageElement.Source => ImageSource;
	bool IImageElement.IsOpaque => false;

	/// <inheritdoc/>
	public string UpdateFormsText(string original, TextTransform transform)
	{
		return TextTransformUtilites.GetTransformedText(original, transform);
	}

	void ITextElement.OnCharacterSpacingPropertyChanged(double oldValue, double newValue)
	{
		InvalidateMeasure();
		label.CharacterSpacing = newValue;
	}

	void ITextElement.OnTextColorPropertyChanged(Color oldValue, Color newValue)
	{
		label.TextColor = newValue;
	}

	void ITextElement.OnTextTransformChanged(TextTransform oldValue, TextTransform newValue)
	{
		InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
		label.TextTransform = newValue;
	}

	void IFontElement.OnFontFamilyChanged(string oldValue, string newValue)
	{
		HandleFontChanged();
		label.FontFamily = newValue;
	}

	void IFontElement.OnFontSizeChanged(double oldValue, double newValue)
	{
		HandleFontChanged();
		label.FontSize = newValue;
	}

	double IFontElement.FontSizeDefaultValueCreator()
	{
		return this.GetDefaultFontSize();
	}

	void IFontElement.OnFontAttributesChanged(FontAttributes oldValue, FontAttributes newValue)
	{
		HandleFontChanged();
		label.FontAttributes = newValue;
	}

	void IFontElement.OnFontAutoScalingEnabledChanged(bool oldValue, bool newValue)
	{
		HandleFontChanged();
		label.FontAutoScalingEnabled = newValue;
	}

	void HandleFontChanged()
	{
		Handler?.UpdateValue(nameof(ITextStyle.Font));
		InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
	}

	void IImageElement.RaiseImageSourcePropertyChanged()
	{
		OnPropertyChanged(ImageElement.ImageSourceProperty.PropertyName);
	}

	bool IImageElement.IsLoading => false;

	bool IImageElement.IsAnimationPlaying => false;

	void IImageElement.OnImageSourceSourceChanged(object sender, EventArgs e)
	{
		ImageElement.ImageSourceSourceChanged(this, e);
	}
}
