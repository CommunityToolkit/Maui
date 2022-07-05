using System.ComponentModel;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Controls;

/// <summary>Avatar content view.</summary>
public class Avatar : ContentView, IAvatarElement, IFontElement, ITextElement, IImageElement
{
	/// <summary>The backing store for the <see cref="IAvatarElement.AvatarBackgroundColor" /> bindable property.</summary>
	public static readonly BindableProperty AvatarBackgroundColorProperty = BindableProperty.Create(nameof(IAvatarElement.AvatarBackgroundColor), typeof(Color), typeof(IAvatarElement), null, propertyChanged: OnBackgroundColorChanged);

	/// <summary>The backing store for the <see cref="IAvatarElement.AvatarHeightRequest" /> bindable property.</summary>
	public static readonly BindableProperty AvatarHeightRequestProperty = BindableProperty.Create(nameof(IAvatarElement.AvatarHeightRequest), typeof(double), typeof(VisualElement), defaultValue: AvatarElement.DefaultHeightRequest, propertyChanged: OnHeightRequestPropertyChanged);

	/// <inheritdoc/>
	public static readonly BindableProperty AvatarPaddingBottomProperty = BindableProperty.Create("AvatarPaddingBottom", typeof(double), typeof(IPaddingElement), default(double), propertyChanged: OnPaddingBottomChanged);

	/// <inheritdoc/>
	public static readonly BindableProperty AvatarPaddingLeftProperty = BindableProperty.Create("AvatarPaddingLeft", typeof(double), typeof(IPaddingElement), default(double), propertyChanged: OnPaddingLeftChanged);

	/// <summary>The backing store for the <see cref="AvatarPadding" /> bindable property.</summary>
	public static readonly BindableProperty AvatarPaddingProperty = BindableProperty.Create(nameof(IPaddingElement.Padding), typeof(Thickness), typeof(IPaddingElement), default(Thickness), propertyChanged: OnPaddingPropertyChanged, defaultValueCreator: PaddingDefaultValueCreator);

	/// <inheritdoc/>
	public static readonly BindableProperty AvatarPaddingRightProperty = BindableProperty.Create("AvatarPaddingRight", typeof(double), typeof(IPaddingElement), default(double), propertyChanged: OnPaddingRightChanged);

	/// <inheritdoc/>
	public static readonly BindableProperty AvatarPaddingTopProperty = BindableProperty.Create("AvatarPaddingTop", typeof(double), typeof(IPaddingElement), default(double), propertyChanged: OnPaddingTopChanged);

	/// <summary>The backing store for the <see cref="AvatarShadow" /> bindable property.</summary>
	public static readonly BindableProperty AvatarShadowProperty = BindableProperty.Create(nameof(IAvatarElement.AvatarShadow), typeof(Shadow), typeof(VisualElement), defaultValue: null, propertyChanged: OnShadowPropertyChanged);

	/// <summary>The backing store for the <see cref="IAvatarElement.AvatarWidthRequest" /> bindable property.</summary>
	public static readonly BindableProperty AvatarWidthRequestProperty = BindableProperty.Create(nameof(IAvatarElement.AvatarWidthRequest), typeof(double), typeof(VisualElement), defaultValue: AvatarElement.DefaultWidthRequest, propertyChanged: OnWidthRequestPropertyChanged);

	/// <summary>The backing store for the <see cref="BorderColor" /> bindable property.</summary>
	public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(nameof(IAvatarElement.BorderColor), typeof(Color), typeof(IAvatarElement), defaultValue: AvatarElement.DefaultBorderColor, propertyChanged: OnBorderColorPropertyChanged);

	/// <summary>The backing store for the <see cref="IAvatarElement.BorderWidth" /> bindable property.</summary>
	public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(nameof(IAvatarElement.BorderWidth), typeof(double), typeof(IAvatarElement), defaultValue: AvatarElement.DefaultBorderWidth, propertyChanged: OnBorderWidthPropertyChanged);

	/// <summary>The backing store for the <see cref="IAvatarElement.CornerRadius" /> bindable property.</summary>
	public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(nameof(IAvatarElement.CornerRadius), typeof(CornerRadius), typeof(ICornerElement), defaultValue: AvatarElement.DefaultCornerRadius, propertyChanged: OnCornerRadiusPropertyChanged);

	/// <summary>The backing store for the <see cref="IFontElement.FontAttributes" /> bindable property.</summary>
	public static readonly BindableProperty FontAttributesProperty = FontElement.FontAttributesProperty;

	/// <summary>The backing store for the <see cref="FontAutoScalingEnabled" /> bindable property.</summary>
	public static readonly BindableProperty FontAutoScalingEnabledProperty = FontElement.FontAutoScalingEnabledProperty;

	/// <summary>The backing store for the <see cref="IFontElement.FontSize" /> bindable property.</summary>
	public static readonly BindableProperty FontSizeProperty = FontElement.FontSizeProperty;

	/// <summary>The backing store for the <see cref="ImageSource" /> bindable property.</summary>
	public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(nameof(ImageSource), typeof(ImageSource), typeof(IImageElement), default(ImageSource), propertyChanged: OnImageSourceChanged);

	/// <summary>The backing store for the <see cref="TextColor" /> bindable property.</summary>
	public static readonly BindableProperty TextColorProperty = TextElement.TextColorProperty;

	/// <summary>The backing store for the <see cref="IAvatarElement.Text" /> bindable property.</summary>
	public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(IAvatarElement.Text), typeof(string), typeof(Avatar), defaultValue: AvatarElement.DefaultText, propertyChanged: OnTextPropertyChanged);

	/// <summary>The backing store for the <see cref="TextTransform" /> bindable property.</summary>
	public static readonly BindableProperty TextTransformProperty = TextElement.TextTransformProperty;

	/// <summary>Initialises a new instance of the <see cref="Avatar"/> class.</summary>
	public Avatar()
	{
		IsEnabled = true;
		avatarBorder.Content = avatarLabel;
		Content = avatarBorder;
		InvalidateMeasure();
	}

	Border avatarBorder { get; } = new Border
	{
		HorizontalOptions = LayoutOptions.Center,
		VerticalOptions = LayoutOptions.Center,
		HeightRequest = AvatarElement.DefaultHeightRequest,
		WidthRequest = AvatarElement.DefaultWidthRequest,
		Stroke = AvatarElement.DefaultBorderColor,
		StrokeThickness = AvatarElement.DefaultBorderWidth,
		Padding = default,
		StrokeShape = new RoundRectangle
		{
			CornerRadius = new CornerRadius(AvatarElement.DefaultCornerRadius.TopLeft, AvatarElement.DefaultCornerRadius.TopRight, AvatarElement.DefaultCornerRadius.BottomLeft, AvatarElement.DefaultCornerRadius.BottomRight),
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
		Text = AvatarElement.DefaultText
	};

	#region Property Events

	/// <summary>On background colour changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnBackgroundColorChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarElement)bindable).OnBackgroundColorChanged((Color)oldValue, (Color)newValue);
	}

	static void OnBorderColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarElement)bindable).OnBorderColorPropertyChanged((Color)oldValue, (Color)newValue);
	}

	/// <summary>On border width property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnBorderWidthPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarElement)bindable).OnBorderWidthPropertyChanged((double)oldValue, (double)newValue);
	}

	/// <summary>On corner radius property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnCornerRadiusPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarElement)bindable).OnCornerRadiusPropertyChanged((CornerRadius)oldValue, (CornerRadius)newValue);
	}

	/// <summary>On height request property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnHeightRequestPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		OnRequestChanged(bindable);
		((IAvatarElement)bindable).OnHeightRequestPropertyChanged((double)oldValue, (double)newValue);
	}

	/// <summary>On image source changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnImageSourceChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarElement)bindable).OnImageSourceChanged((ImageSource)oldValue, (ImageSource)newValue);
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
		((IAvatarElement)bindable).OnPaddingPropertyChanged((Thickness)oldValue, (Thickness)newValue);
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
		((IAvatarElement)bindable).OnShadowPropertyChanged((Shadow)oldValue, (Shadow)newValue);
	}

	/// <summary>On text property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarElement)bindable).OnTextPropertyChanged((string)oldValue, (string)newValue);
	}

	/// <summary>On width request property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	static void OnWidthRequestPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		OnRequestChanged(bindable);
		((IAvatarElement)bindable).OnWidthRequestPropertyChanged((double)oldValue, (double)newValue);
	}

	/// <summary>Padding default value creator.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <returns>Default padding value.</returns>
	static object PaddingDefaultValueCreator(BindableObject bindable)
	{
		return ((IPaddingElement)bindable).PaddingDefaultValueCreator();
	}

	#endregion Property Events

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
	}

	void IImageElement.RaiseImageSourcePropertyChanged()
	{
		OnPropertyChanged(ImageElement.ImageSourceProperty.PropertyName);
	}

	void HandleImageChanged(ImageSource newValue)
	{
		avatarImage.Source = newValue;
		avatarBorder.Content = newValue is not null ? avatarImage : avatarLabel;
		HandleCornerRadiusChanged();
	}

	#endregion Image Element

	#region Avatar ControlView

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
	public Shadow AvatarShadow
	{
		get => (Shadow)GetValue(AvatarShadowProperty);
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

	/// <summary>Gets or sets a value of the avatar corder radius property.</summary>
	public CornerRadius CornerRadius
	{
		get => (CornerRadius)GetValue(CornerRadiusProperty);
		set => SetValue(CornerRadiusProperty, value);
	}

	CornerRadius IAvatarElement.CornerRadiusDefaultValue => (CornerRadius)CornerRadiusProperty.DefaultValue;

	/// <summary>Gets or sets a value of the avatar text property.</summary>
	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	string IAvatarElement.TextDefaultValue => (string)TextProperty.DefaultValue;

	bool IAvatarElement.IsTextSet()
	{
		return IsSet(TextProperty);
	}

	void IAvatarElement.OnBackgroundColorChanged(Color oldValue, Color newValue)
	{
		avatarBorder.Background = newValue;
	}

	void IAvatarElement.OnBorderColorPropertyChanged(Color oldValue, Color newValue)
	{
		avatarBorder.Stroke = newValue;
	}

	void IAvatarElement.OnBorderWidthPropertyChanged(double oldValue, double newValue)
	{
		avatarBorder.StrokeThickness = newValue;
	}

	void IAvatarElement.OnCornerRadiusPropertyChanged(CornerRadius oldValue, CornerRadius newValue)
	{
		avatarBorder.StrokeShape = new RoundRectangle { CornerRadius = newValue };
	}

	void IAvatarElement.OnHeightRequestPropertyChanged(double oldValue, double newValue)
	{
		avatarBorder.HeightRequest = newValue;
		OnSizeAllocated(AvatarWidthRequest, newValue);
	}

	void IAvatarElement.OnImageSourceChanged(ImageSource oldValue, ImageSource newValue)
	{
		HandleImageChanged(newValue);
	}

	void IAvatarElement.OnPaddingPropertyChanged(Thickness oldValue, Thickness newValue)
	{
		avatarBorder.Padding = newValue;
	}

	/// <summary>On avatar shadow changed.</summary>
	/// <remarks>This allows for the avatar to have a shadow, which is different from the parent.</remarks>
	/// <param name="oldValue">Old value.</param>
	/// <param name="newValue">New value.</param>
	void IAvatarElement.OnShadowPropertyChanged(Shadow oldValue, Shadow newValue)
	{
		avatarBorder.Shadow = newValue;
	}

	void IAvatarElement.OnTextPropertyChanged(string oldValue, string newValue)
	{
		avatarLabel.Text = newValue;
	}

	void IAvatarElement.OnWidthRequestPropertyChanged(double oldValue, double newValue)
	{
		avatarBorder.WidthRequest = newValue;
		OnSizeAllocated(newValue, AvatarHeightRequest);
	}

	Thickness IAvatarElement.PaddingDefaultValueCreator()
	{
		return default;
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

	#endregion Avatar ControlView
}