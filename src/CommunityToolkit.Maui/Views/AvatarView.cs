using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;
using Font = Microsoft.Maui.Font;

namespace CommunityToolkit.Maui.Views;

/// <summary>Avatar content view.</summary>
public class AvatarView : ContentView, IAvatarView, IFontElement, ITextElement
{
	/// <summary>The backing store for the <see cref="AvatarBackgroundColor" /> bindable property.</summary>
	public static readonly BindableProperty AvatarBackgroundColorProperty = BindableProperty.Create(nameof(AvatarBackgroundColor), typeof(Color), typeof(IAvatarView), Colors.Transparent, propertyChanged: OnBackgroundColorChanged);

	/// <summary>The backing store for the <see cref="AvatarHeightRequest" /> bindable property.</summary>
	public static readonly BindableProperty AvatarHeightRequestProperty = BindableProperty.Create(nameof(IAvatarView.AvatarHeightRequest), typeof(double), typeof(VisualElement), defaultValue: AvatarViewDefaults.DefaultHeightRequest, propertyChanged: OnHeightRequestPropertyChanged);

	/// <summary>The backing store for the <see cref="AvatarPaddingBottomProperty" /> bindable property.</summary>
	public static readonly BindableProperty AvatarPaddingBottomProperty = BindableProperty.Create("AvatarPaddingBottom", typeof(double), typeof(IPaddingElement), default(double), propertyChanged: OnPaddingBottomChanged);

	/// <summary>The backing store for the <see cref="AvatarPaddingLeftProperty" /> bindable property.</summary>
	public static readonly BindableProperty AvatarPaddingLeftProperty = BindableProperty.Create("AvatarPaddingLeft", typeof(double), typeof(IPaddingElement), default(double), propertyChanged: OnPaddingLeftChanged);

	/// <summary>The backing store for the <see cref="AvatarPadding" /> bindable property.</summary>
	public static readonly BindableProperty AvatarPaddingProperty = BindableProperty.Create(nameof(IPaddingElement.Padding), typeof(Thickness), typeof(IPaddingElement), default(Thickness), propertyChanged: OnPaddingPropertyChanged, defaultValueCreator: PaddingDefaultValueCreator);

	/// <summary>The backing store for the <see cref="AvatarPaddingRightProperty" /> bindable property.</summary>
	public static readonly BindableProperty AvatarPaddingRightProperty = BindableProperty.Create("AvatarPaddingRight", typeof(double), typeof(IPaddingElement), default(double), propertyChanged: OnPaddingRightChanged);

	/// <summary>The backing store for the <see cref="AvatarPaddingTopProperty" /> bindable property.</summary>
	public static readonly BindableProperty AvatarPaddingTopProperty = BindableProperty.Create("AvatarPaddingTop", typeof(double), typeof(IPaddingElement), default(double), propertyChanged: OnPaddingTopChanged);

	/// <summary>The backing store for the <see cref="AvatarShadow" /> bindable property.</summary>
	public static readonly BindableProperty AvatarShadowProperty = BindableProperty.Create(nameof(AvatarShadow), typeof(IShadow), typeof(VisualElement), defaultValue: null, propertyChanged: OnShadowPropertyChanged);

	/// <summary>The backing store for the <see cref="AvatarWidthRequest" /> bindable property.</summary>
	public static readonly BindableProperty AvatarWidthRequestProperty = BindableProperty.Create(nameof(IAvatarView.AvatarWidthRequest), typeof(double), typeof(VisualElement), defaultValue: AvatarViewDefaults.DefaultWidthRequest, propertyChanged: OnWidthRequestPropertyChanged);

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
		avatarBorder.Content = avatarLabel;
		Content = avatarBorder;
		InvalidateMeasure();
	}

	/// <summary>Gets or sets a value of the avatar background colour property.</summary>
	public Color AvatarBackgroundColor
	{
		get => (Color)GetValue(AvatarBackgroundColorProperty);
		set => SetValue(AvatarBackgroundColorProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar height request property.</summary>
	public double AvatarHeightRequest
	{
		get => (double)GetValue(AvatarHeightRequestProperty);
		set => SetValue(AvatarHeightRequestProperty, value);
	}

	/// <summary>Gets or sets the avatar padding property.</summary>
	public Thickness AvatarPadding
	{
		get => (Thickness)GetValue(PaddingElement.PaddingProperty);
		set => SetValue(PaddingElement.PaddingProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar shadow property.</summary>
	public IShadow AvatarShadow
	{
		get => (IShadow)GetValue(AvatarShadowProperty);
		set => SetValue(AvatarShadowProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar width request property.</summary>
	public double AvatarWidthRequest
	{
		get => (double)GetValue(AvatarWidthRequestProperty);
		set => SetValue(AvatarWidthRequestProperty, value);
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

	/// <inheritdoc/>
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

	CornerRadius IAvatarView.CornerRadiusDefaultValue => (CornerRadius)CornerRadiusProperty.DefaultValue;

	/// <summary>Gets or sets the avatar font.</summary>
	public Font Font { get; set; } = Font.SystemFontOfSize(AvatarViewDefaults.FontSize);

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

	/// <inheritdoc/>
	[TypeConverter(typeof(ImageSourceConverter))]
	public ImageSource ImageSource
	{
		get => (ImageSource)GetValue(ImageElement.ImageSourceProperty);
		set => SetValue(ImageElement.ImageSourceProperty, value);
	}

	bool IImageSourcePart.IsAnimationPlaying => false;

	bool IImageSource.IsEmpty => false;

	IImageSource IImageSourcePart.Source => ImageSource;

	/// <summary>Gets or sets a value of the avatar text property.</summary>
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

	string IAvatarView.TextDefaultValue => (string)TextProperty.DefaultValue;

	/// <inheritdoc/>
	public TextTransform TextTransform
	{
		get => (TextTransform)GetValue(TextElement.TextTransformProperty);
		set => SetValue(TextElement.TextTransformProperty, value);
	}

	Border avatarBorder { get; } = new Border
	{
		HorizontalOptions = LayoutOptions.Center,
		VerticalOptions = LayoutOptions.Center,
		HeightRequest = AvatarViewDefaults.DefaultHeightRequest,
		WidthRequest = AvatarViewDefaults.DefaultWidthRequest,
		Stroke = AvatarViewDefaults.DefaultBorderColor,
		StrokeThickness = AvatarViewDefaults.DefaultBorderWidth,
		Padding = default,
		StrokeShape = new RoundRectangle
		{
			CornerRadius = new CornerRadius(AvatarViewDefaults.DefaultCornerRadius.TopLeft, AvatarViewDefaults.DefaultCornerRadius.TopRight, AvatarViewDefaults.DefaultCornerRadius.BottomLeft, AvatarViewDefaults.DefaultCornerRadius.BottomRight),
		},
	};

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

	double IFontElement.FontSizeDefaultValueCreator()
	{
		return this.GetDefaultFontSize();
	}

	bool IAvatarView.IsTextSet()
	{
		return IsSet(TextProperty);
	}

	void IAvatarView.OnBackgroundColorChanged(Color oldValue, Color newValue)
	{
		avatarBorder.Background = newValue;
	}

	void IAvatarView.OnBorderColorPropertyChanged(Color oldValue, Color newValue)
	{
		avatarBorder.Stroke = newValue;
	}

	void IAvatarView.OnBorderWidthPropertyChanged(double oldValue, double newValue)
	{
		avatarBorder.StrokeThickness = newValue;
	}

	void ITextElement.OnCharacterSpacingPropertyChanged(double oldValue, double newValue)
	{
		InvalidateMeasure();
		avatarLabel.CharacterSpacing = newValue;
	}

	void IAvatarView.OnCornerRadiusPropertyChanged(CornerRadius oldValue, CornerRadius newValue)
	{
		avatarBorder.StrokeShape = new RoundRectangle { CornerRadius = newValue };
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

	void IAvatarView.OnHeightRequestPropertyChanged(double oldValue, double newValue)
	{
		avatarBorder.HeightRequest = newValue;
		OnSizeAllocated(AvatarWidthRequest, newValue);
	}

	void IAvatarView.OnImageSourceChanged(object oldValue, object newValue)
	{
		HandleImageChanged((ImageSource)newValue);
	}

	void IAvatarView.OnPaddingPropertyChanged(Thickness oldValue, Thickness newValue)
	{
		avatarBorder.Padding = newValue;
	}

	/// <summary>On avatar shadow changed.</summary>
	/// <remarks>This allows for the avatar to have a shadow, which is different from the parent.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void IAvatarView.OnShadowPropertyChanged(object oldValue, object newValue)
	{
		avatarBorder.Shadow = (Shadow)newValue;
	}

	void ITextElement.OnTextColorPropertyChanged(Color oldValue, Color newValue)
	{
		avatarLabel.TextColor = newValue;
	}

	void IAvatarView.OnTextPropertyChanged(string oldValue, string newValue)
	{
		avatarLabel.Text = newValue;
	}

	void ITextElement.OnTextTransformChanged(TextTransform oldValue, TextTransform newValue)
	{
		InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
		avatarLabel.TextTransform = newValue;
	}

	void IAvatarView.OnWidthRequestPropertyChanged(double oldValue, double newValue)
	{
		avatarBorder.WidthRequest = newValue;
		OnSizeAllocated(newValue, AvatarHeightRequest);
	}

	Thickness IAvatarView.PaddingDefaultValueCreator()
	{
		return default;
	}

	/// <inheritdoc/>
	public string UpdateFormsText(string original, TextTransform transform)
	{
		return TextTransformUtilites.GetTransformedText(original, transform);
	}

	void IImageSourcePart.UpdateIsLoading(bool isLoading)
	{
		if (!isLoading && wasImageLoading)
		{
			Handler?.UpdateValue(nameof(AvatarView));
		}

		wasImageLoading = isLoading;
	}

	/// <summary>On background colour changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnBackgroundColorChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarView)bindable).OnBackgroundColorChanged((Color)oldValue, (Color)newValue);
	}

	static void OnBorderColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarView)bindable).OnBorderColorPropertyChanged((Color)oldValue, (Color)newValue);
	}

	/// <summary>On border width property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnBorderWidthPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarView)bindable).OnBorderWidthPropertyChanged((double)oldValue, (double)newValue);
	}

	/// <summary>On corner radius property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnCornerRadiusPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarView)bindable).OnCornerRadiusPropertyChanged((CornerRadius)oldValue, (CornerRadius)newValue);
	}

	/// <summary>On height request property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnHeightRequestPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		OnRequestChanged(bindable);
		((IAvatarView)bindable).OnHeightRequestPropertyChanged((double)oldValue, (double)newValue);
	}

	/// <summary>On image source changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnImageSourceChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarView)bindable).OnImageSourceChanged((ImageSource)oldValue, (ImageSource)newValue);
	}

	static void OnPaddingBottomChanged(BindableObject bindable, object oldValue, object newValue)
	{
		Thickness padding = (Thickness)bindable.GetValue(AvatarPaddingProperty);
		padding.Bottom = (double)newValue;
		bindable.SetValue(AvatarPaddingProperty, padding);
	}

	static void OnPaddingLeftChanged(BindableObject bindable, object oldValue, object newValue)
	{
		Thickness padding = (Thickness)bindable.GetValue(AvatarPaddingProperty);
		padding.Left = (double)newValue;
		bindable.SetValue(AvatarPaddingProperty, padding);
	}

	/// <summary>On padding property changed event.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnPaddingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarView)bindable).OnPaddingPropertyChanged((Thickness)oldValue, (Thickness)newValue);
	}

	static void OnPaddingRightChanged(BindableObject bindable, object oldValue, object newValue)
	{
		Thickness padding = (Thickness)bindable.GetValue(AvatarPaddingProperty);
		padding.Right = (double)newValue;
		bindable.SetValue(AvatarPaddingProperty, padding);
	}

	static void OnPaddingTopChanged(BindableObject bindable, object oldValue, object newValue)
	{
		Thickness padding = (Thickness)bindable.GetValue(AvatarPaddingProperty);
		padding.Top = (double)newValue;
		bindable.SetValue(AvatarPaddingProperty, padding);
	}

	/// <summary>On visual element width or height request property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	static void OnRequestChanged(BindableObject bindable)
	{
		LayoutConstraint constraint = LayoutConstraint.None;
		VisualElement element = (VisualElement)bindable;
		if (element.WidthRequest >= 0 && element.MinimumWidthRequest >= 0)
		{
			constraint |= LayoutConstraint.HorizontallyFixed;
		}
		if (element.HeightRequest >= 0 && element.MinimumHeightRequest >= 0)
		{
			constraint |= LayoutConstraint.VerticallyFixed;
		}

		element.SelfConstraint = constraint;

		if (element is IView fe)
		{
			fe.Handler?.UpdateValue(nameof(IView.Width));
			fe.Handler?.UpdateValue(nameof(IView.Height));
			fe.Handler?.UpdateValue(nameof(IView.MinimumHeight));
			fe.Handler?.UpdateValue(nameof(IView.MinimumWidth));
			fe.Handler?.UpdateValue(nameof(IView.MaximumHeight));
			fe.Handler?.UpdateValue(nameof(IView.MaximumWidth));
		}

		((VisualElement)bindable).InvalidateMeasureInternal(InvalidationTrigger.SizeRequestChanged);
	}

	static void OnShadowPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarView)bindable).OnShadowPropertyChanged((Shadow)oldValue, (Shadow)newValue);
	}

	/// <summary>On text property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarView)bindable).OnTextPropertyChanged((string)oldValue, (string)newValue);
	}

	/// <summary>On width request property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnWidthRequestPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		OnRequestChanged(bindable);
		((IAvatarView)bindable).OnWidthRequestPropertyChanged((double)oldValue, (double)newValue);
	}

	/// <summary>Padding default value creator.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <returns>Default padding value.</returns>
	static object PaddingDefaultValueCreator(BindableObject bindable)
	{
		return ((IPaddingElement)bindable).PaddingDefaultValueCreator();
	}

	void HandleCornerRadiusChanged()
	{
		CornerRadius cornerRadius = new();
		if (CornerRadius != cornerRadius)
		{
			avatarBorder.StrokeShape = new RoundRectangle { CornerRadius = cornerRadius };
		}

		avatarBorder.StrokeShape = new RoundRectangle { CornerRadius = CornerRadius };
	}

	void HandleFontChanged()
	{
		Handler?.UpdateValue(nameof(ITextStyle.Font));
		InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
	}

	void HandleImageChanged(ImageSource newValue)
	{
		avatarImage.Source = newValue;
		avatarBorder.Content = newValue is not null ? avatarImage : avatarLabel;
		HandleCornerRadiusChanged();
	}
}