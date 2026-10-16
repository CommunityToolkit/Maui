using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Shapes;
using ToolkitDispatcherExtensions = CommunityToolkit.Maui.Core.Extensions.DispatcherExtensions;

namespace CommunityToolkit.Maui.Views;

/// <summary>AvatarView control.</summary>
/// <remarks>
/// AvatarView changes the defaults of these <see cref="Border"/> and <see cref="View"/> properties: <see cref="VisualElement.WidthRequest"/> and
/// <see cref="VisualElement.HeightRequest"/> are 48, <see cref="Border.Padding"/> is 1, <see cref="Border.Stroke"/> is white,
/// <see cref="Border.StrokeThickness"/> is 1, <see cref="Border.StrokeShape"/> is a <see cref="RoundRectangle"/> with a corner radius of 24, and
/// <see cref="View.HorizontalOptions"/> and <see cref="View.VerticalOptions"/> are <see cref="LayoutOptions.Center"/>. Any value given to one of these
/// properties replaces its default, whether it comes from a local assignment, a binding, a <see cref="Style"/> (explicit or implicit), a trigger or a
/// visual state, and the default comes back when that value is removed.
/// </remarks>
public partial class AvatarView : Border, IAvatarView
{
	// The Border and View properties whose defaults AvatarView changes (see SetAvatarViewDefault).
	static readonly BindableProperty[] propertiesWithAvatarViewDefaults =
	[
		HorizontalOptionsProperty,
		VerticalOptionsProperty,
		HeightRequestProperty,
		WidthRequestProperty,
		PaddingProperty,
		StrokeProperty,
		StrokeThicknessProperty,
		StrokeShapeProperty,
	];

	readonly AvatarImage avatarImage = new()
	{
		Aspect = Aspect.AspectFill,
	};

	readonly Label avatarLabel = new()
	{
		HorizontalTextAlignment = TextAlignment.Center,
		VerticalTextAlignment = TextAlignment.Center,
		Text = AvatarViewDefaults.Text,
	};

	// The default StrokeShape, while it is the StrokeShape (see HandleAvatarViewDefault).
	RoundRectangle? defaultStrokeShape;
	bool wasImageLoading;

	/// <summary>
	/// Initializes a new instance of the <see cref="AvatarView"/> class.
	/// </summary>
	public AvatarView()
	{
		PropertyChanged += HandlePropertyChanged;

		foreach (var property in propertiesWithAvatarViewDefaults)
		{
			SetAvatarViewDefault(property);
		}

		Content = avatarLabel;
		avatarImage.SetBinding(WidthRequestProperty, BindingBase.Create<VisualElement, double>(static p => p.WidthRequest, source: this));
		avatarImage.SetBinding(HeightRequestProperty, BindingBase.Create<VisualElement, double>(static p => p.HeightRequest, source: this));
		avatarImage.SourceLoaded += HandleImageSourceLoaded;
		avatarImage.SourceFailedToLoad += HandleImageSourceFailedToLoad;
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
	/// <remarks>
	/// While an image source is set, the control shows the image instead of its <see cref="Text"/>. When the image source fails to load (for example a
	/// URL that cannot be reached), the control shows its <see cref="Text"/> again, until the image source loads or another image source is set.
	/// </remarks>
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


	void IImageSourcePart.UpdateIsLoading(bool isLoading)
	{
		if (!isLoading && wasImageLoading)
		{
			Handler?.UpdateValue(nameof(AvatarView));
		}

		wasImageLoading = isLoading;
	}

	static void OnCharacterSpacingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var avatarView = (AvatarView)bindable;
		avatarView.OnCharacterSpacingChanged((double)oldValue, (double)newValue);
	}

	static void OnFontAttributesBindablePropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var avatarView = (AvatarView)bindable;
		avatarView.OnFontAttributesChanged((FontAttributes)oldValue, (FontAttributes)newValue);
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

	void OnCharacterSpacingChanged(double oldValue, double newValue)
	{
		InvalidateMeasure();
		avatarLabel.CharacterSpacing = newValue;
	}

	void OnFontAttributesChanged(FontAttributes oldValue, FontAttributes newValue)
	{
		HandleFontChanged();
		avatarLabel.FontAttributes = newValue;
	}

	void OnFontAutoScalingEnabledChanged(bool oldValue, bool newValue)
	{
		HandleFontChanged();
		avatarLabel.FontAutoScalingEnabled = newValue;
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

	// The image source loaded: the image replaces the text that a failed load left (the source itself can change, a UriImageSource's Uri for example).
	void HandleImageSourceLoaded(object? sender, EventArgs e) => ToolkitDispatcherExtensions.DispatchIfRequired(Dispatcher, () =>
	{
		if (avatarImage.Source is not null && ReferenceEquals(Content, avatarLabel))
		{
			RefreshAvatarImage();
			Content = avatarImage;
		}
	});

	// The image source failed to load (for example a URL that cannot be reached): the image would stay empty, so the text is shown instead.
	void HandleImageSourceFailedToLoad(object? sender, EventArgs e) => ToolkitDispatcherExtensions.DispatchIfRequired(Dispatcher, () =>
	{
		if (ReferenceEquals(Content, avatarImage))
		{
			Content = avatarLabel;
		}
	});

	// Gives the property its AvatarView default as a value from the handler (Element.SetValueFromRenderer), not as a local value: a local value takes
	// precedence over every Style setter, so no Style could size an AvatarView or change its stroke, while a value from the handler only stays until
	// another value replaces it (a local value, a binding, a Style, a trigger or a visual state).
	void SetAvatarViewDefault(BindableProperty property)
	{
		object defaultValue = property.PropertyName switch
		{
			nameof(HorizontalOptions) or nameof(VerticalOptions) => LayoutOptions.Center,
			nameof(HeightRequest) => AvatarViewDefaults.HeightRequest,
			nameof(WidthRequest) => AvatarViewDefaults.WidthRequest,
			nameof(Padding) => AvatarViewDefaults.Padding,
			// AvatarViewDefaults.BorderColor, as an immutable brush: Border registers a mutable brush with the AvatarView, and does not unregister a brush
			// from the handler when another value replaces it.
			nameof(Stroke) => Brush.White,
			nameof(StrokeThickness) => AvatarViewDefaults.BorderWidth,
			nameof(StrokeShape) => defaultStrokeShape = new RoundRectangle { CornerRadius = AvatarViewDefaults.CornerRadius },
			_ => throw new ArgumentOutOfRangeException(nameof(property), property.PropertyName, "AvatarView does not change the default of this property.")
		};

		SetValueFromRenderer(property, defaultValue);
	}

	void HandleAvatarViewDefault(string? propertyName)
	{
		foreach (var property in propertiesWithAvatarViewDefaults)
		{
			if (property.PropertyName == propertyName)
			{
				HandleAvatarViewDefault(property);
				return;
			}
		}
	}

	void HandleAvatarViewDefault(BindableProperty property)
	{
		if (property == StrokeShapeProperty && defaultStrokeShape is not null && !ReferenceEquals(StrokeShape, defaultStrokeShape))
		{
			// Another value replaced the default shape, or it was cleared. Border removes the previous StrokeShape from its logical children, but it reads
			// that shape once a value from the handler is already discarded: the default shape would stay a logical child.
			RemoveLogicalChild(defaultStrokeShape);
			defaultStrokeShape = null;
		}

		if (!IsSet(property))
		{
			// The value that replaced the default was removed (ClearValue, or a Style, a trigger or a visual state that no longer applies): the default comes back.
			SetAvatarViewDefault(property);

			if (property == StrokeShapeProperty && defaultStrokeShape is not null)
			{
				// Border adds the StrokeShape to its logical children when the notification of the removed value ends, after this handler: the default
				// shape, which SetValueFromRenderer has just added, would be there twice.
				RemoveLogicalChild(defaultStrokeShape);
			}
		}
	}

	void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		HandleAvatarViewDefault(e.PropertyName);

		// Ensure avatarImage is clipped to the bounds of the AvatarView whenever its Height, Width, CornerRadius, Border, StrokeThickness, StrokeShape and Padding properties change
		if (e.PropertyName == HeightProperty.PropertyName
			|| e.PropertyName == WidthProperty.PropertyName
			|| e.PropertyName == PaddingProperty.PropertyName
			|| e.PropertyName == BorderWidthProperty.PropertyName
			|| e.PropertyName == CornerRadiusProperty.PropertyName
			|| e.PropertyName == StrokeThicknessProperty.PropertyName
			|| e.PropertyName == StrokeShapeProperty.PropertyName
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
			var imageWidth = Width - (StrokeThickness * 2) - Padding.Left - Padding.Right;
			var imageHeight = Height - (StrokeThickness * 2) - Padding.Top - Padding.Bottom;
			avatarImage.WidthRequest = imageWidth;
			avatarImage.HeightRequest = imageHeight;

			if (!OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst() && !OperatingSystem.IsMacOS())
			{
				// The clip is in the image's own coordinates, and the image is already laid out inside the stroke and the padding:
				// the clip starts at the image's origin
				avatarImageClipGeometry = new RoundRectangleGeometry
				{
					CornerRadius = CornerRadius,
					Rect = new(0, 0, imageWidth, imageHeight)
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

	// The image of an AvatarView. The image handler reports through IImageSourcePartEvents whether the image source loaded: MAUI's Image has no
	// event for a source that fails to load, and then shows nothing. Partial for CsWinRT on Windows (CsWinRT1028: Image implements WinRT interfaces).
	sealed partial class AvatarImage : Image, IImageSourcePartEvents
	{
		public event EventHandler? SourceLoaded;

		public event EventHandler? SourceFailedToLoad;

		void IImageSourcePartEvents.LoadingStarted()
		{
		}

		// Not successful: the load was canceled or replaced by the load of another source, which reports for itself.
		void IImageSourcePartEvents.LoadingCompleted(bool successful)
		{
			if (successful)
			{
				SourceLoaded?.Invoke(this, EventArgs.Empty);
			}
		}

		void IImageSourcePartEvents.LoadingFailed(Exception exception) => SourceFailedToLoad?.Invoke(this, EventArgs.Empty);
	}
}