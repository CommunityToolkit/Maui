using System.ComponentModel;
using Microsoft.Maui.Controls.Internals;

namespace CommunityToolkit.Maui.Controls;

/// <summary>Avatar box view.</summary>
public partial class AvatarViewBoxView : BoxView, ITextElement, IFontElement, IImageElement
{
	/// <summary>The backing store for the <see cref="Text" /> bindable property.</summary>
	public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(AvatarViewBoxView), defaultValue: "?", propertyChanged: (bindable, _, __) => ((AvatarViewBoxView)bindable).InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged));

	/// <summary>Initialises a new instance of the Avatar Box View class.</summary>
	public AvatarViewBoxView()
	{
		CornerRadius = 10;
		WidthRequest = 100;
		HeightRequest = 100;
		Text = "?";
	}

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
	}

	void ITextElement.OnTextColorPropertyChanged(Color oldValue, Color newValue)
	{
		// Left intentionally blank.
	}

	void ITextElement.OnTextTransformChanged(TextTransform oldValue, TextTransform newValue)
	{
		InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
	}

	void IFontElement.OnFontFamilyChanged(string oldValue, string newValue)
	{
		HandleFontChanged();
	}

	void IFontElement.OnFontSizeChanged(double oldValue, double newValue)
	{
		HandleFontChanged();
	}

	double IFontElement.FontSizeDefaultValueCreator()
	{
		return this.GetDefaultFontSize();
	}

	void IFontElement.OnFontAttributesChanged(FontAttributes oldValue, FontAttributes newValue)
	{
		HandleFontChanged();
	}

	void IFontElement.OnFontAutoScalingEnabledChanged(bool oldValue, bool newValue)
	{
		HandleFontChanged();
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
