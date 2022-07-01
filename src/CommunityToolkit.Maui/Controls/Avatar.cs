using System.ComponentModel;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Controls;
/// <summary>Avatar content view.</summary>
public class Avatar : ContentView, IAvatarElement, IFontElement, ITextElement, IImageElement
{
	/// <summary>Initialises a new instance of the <see cref="Avatar"/> class.</summary>
	public Avatar()
	{
		IsEnabled = true;
		grid.Add(avatarLabel);
		grid.Add(avatarImage);
		HandleImageChanged();
		Content = avatarFrame;
		InvalidateMeasure();
	}

	Border avatarFrame { get; } = new Border
	{
		HorizontalOptions = LayoutOptions.Center,
		Padding = new Thickness(0.01),
		Stroke = new LinearGradientBrush
		{
			EndPoint = new Point(0, 1),
			GradientStops = new GradientStopCollection
			{
				new GradientStop { Color = Colors.Orange, Offset = 0.1f },
				new GradientStop { Color = Colors.Brown, Offset = 1.0f }
			},
		},
	};

	Grid grid { get; } = new Grid();

	Label avatarLabel { get; } = new Label
	{
		HorizontalTextAlignment = TextAlignment.Center,
		VerticalTextAlignment = TextAlignment.Center,
		Text = AvatarElement.DefaultText
	};

	Image avatarImage { get; } = new Image
	{
		Aspect = Aspect.AspectFill,
	};

	#region Text element

	/// <inheritdoc/>
	public double CharacterSpacing
	{
		get => (double)GetValue(TextElement.CharacterSpacingProperty);
		set => SetValue(TextElement.CharacterSpacingProperty, value);
	}

	/// <inheritdoc/>
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

	void ITextElement.OnCharacterSpacingPropertyChanged(double oldValue, double newValue)
	{
		InvalidateMeasure();
		avatarLabel.CharacterSpacing = newValue;
	}

	void ITextElement.OnTextColorPropertyChanged(Color oldValue, Color newValue)
	{
		avatarLabel.TextColor = newValue;
	}

	void ITextElement.OnTextTransformChanged(TextTransform oldValue, TextTransform newValue)
	{
		InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
		avatarLabel.TextTransform = newValue;
	}

	/// <inheritdoc/>
	public string UpdateFormsText(string original, TextTransform transform)
	{
		return TextTransformUtilites.GetTransformedText(original, transform);
	}

	#endregion Text element

	#region Font element

	/// <inheritdoc/>
	public FontAttributes FontAttributes
	{
		get => (FontAttributes)GetValue(FontElement.FontAttributesProperty);
		set => SetValue(FontElement.FontAttributesProperty, value);
	}

	/// <inheritdoc/>
	public bool FontAutoScalingEnabled
	{
		get => (bool)GetValue(FontElement.FontAutoScalingEnabledProperty);
		set => SetValue(FontElement.FontAutoScalingEnabledProperty, value);
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

	double IFontElement.FontSizeDefaultValueCreator()
	{
		return this.GetDefaultFontSize();
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

	void HandleFontChanged()
	{
		Handler?.UpdateValue(nameof(ITextStyle.Font));
		InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
	}

	#endregion Font element

	#region Image Element

	Aspect IImageElement.Aspect => Aspect.AspectFit;

	/// <inheritdoc/>
	[TypeConverter(typeof(ImageSourceConverter))]
	public ImageSource ImageSource
	{
		get => (ImageSource)GetValue(ImageElement.ImageSourceProperty);
		set => SetValue(ImageElement.ImageSourceProperty, value);
	}

	bool IImageElement.IsAnimationPlaying => false;
	bool IImageElement.IsLoading => false;
	bool IImageElement.IsOpaque => false;
	ImageSource IImageElement.Source => ImageSource;

	void IImageElement.OnImageSourceSourceChanged(object sender, EventArgs e)
	{
		ImageElement.ImageSourceSourceChanged(this, e);
		HandleImageChanged();
	}
	void IImageElement.RaiseImageSourcePropertyChanged()
	{
		OnPropertyChanged(ImageElement.ImageSourceProperty.PropertyName);
	}

	/// <inheritdoc/>
	protected override void OnBindingContextChanged()
	{
		ImageSource bindingImage = ImageSource;
		if (bindingImage is not null)
		{
			SetInheritedBindingContext(bindingImage, BindingContext);
			HandleImageChanged();
		}

		base.OnBindingContextChanged();
	}

	void HandleImageChanged()
	{
		avatarImage.Source = ImageSource;
		avatarFrame.Content = ImageSource is not null ? avatarImage : avatarLabel;
		HandleCornerRadiusChanged();
	}

	#endregion Image Element

	#region Avatar ControlView

	/// <summary>Gets or sets a value of the avatar text property.</summary>
	public string Text
	{
		get => (string)GetValue(AvatarElement.TextProperty);
		set => SetValue(AvatarElement.TextProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar corder radius property.</summary>
	public CornerRadius CornerRadius
	{
		get => (CornerRadius)GetValue(AvatarElement.CornerRadiusProperty);
		set => SetValue(AvatarElement.CornerRadiusProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar background colour property.</summary>
	public Color AvatarBackgroundColor
	{
		get => (Color)GetValue(AvatarElement.AvatarBackgroundColorProperty);
		set => SetValue(AvatarElement.AvatarBackgroundColorProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar border width.</summary>
	public double BorderWidth
	{
		get => (double)GetValue(AvatarElement.BorderWidthProperty);
		set => SetValue(AvatarElement.BorderWidthProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar width request property.</summary>
	public double AvatarWidthRequest
	{
		get => (double)GetValue(AvatarElement.AvatarWidthRequestProperty);
		set => SetValue(AvatarElement.AvatarWidthRequestProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar height request property.</summary>
	public double AvatarHeightRequest
	{
		get => (double)GetValue(AvatarElement.AvatarHeightRequestProperty);
		set => SetValue(AvatarElement.AvatarHeightRequestProperty, value);
	}

	string IAvatarElement.TextDefaultValue => (string)AvatarElement.TextProperty.DefaultValue;
	CornerRadius IAvatarElement.CornerRadiusDefaultValue => (CornerRadius)AvatarElement.CornerRadiusProperty.DefaultValue;

	bool IAvatarElement.IsTextSet()
	{
		return IsSet(AvatarElement.TextProperty);
	}

	void IAvatarElement.OnTextPropertyChanged(string oldValue, string newValue)
	{
		avatarLabel.Text = newValue;
	}

	void IAvatarElement.OnCornerRadiusPropertyChanged(CornerRadius oldValue, CornerRadius newValue)
	{
		avatarFrame.StrokeShape = new RoundRectangle { CornerRadius = newValue };
	}

	void IAvatarElement.OnImageSourceChanged(ImageSource oldValue, ImageSource newValue)
	{
		HandleImageChanged();
	}

	void IAvatarElement.OnBackgroundColorChanged(Color oldValue, Color newValue)
	{
		avatarFrame.Background = newValue;
	}

	void IAvatarElement.OnBorderWidthPropertyChanged(double oldValue, double newValue)
	{
		avatarFrame.StrokeThickness = newValue;
	}

	void IAvatarElement.OnWidthRequestPropertyChanged(double oldValue, double newValue)
	{
		avatarFrame.WidthRequest = newValue;
	}

	void IAvatarElement.OnHeightRequestPropertyChanged(double oldValue, double newValue)
	{
		avatarFrame.HeightRequest = newValue;
	}

	void HandleCornerRadiusChanged()
	{
		CornerRadius cornerRadius = new();
		if (CornerRadius != cornerRadius)
		{
			avatarFrame.StrokeShape = new RoundRectangle { CornerRadius = cornerRadius };
		}

		avatarFrame.StrokeShape = new RoundRectangle { CornerRadius = CornerRadius };
	}

	#endregion Avatar ControlView
}