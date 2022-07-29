using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views;

/// <summary>AvatarView control.</summary>
public class AvatarView : Border, IAvatarView, IBorderElement, IFontElement, ITextElement, IImageElement, ITextAlignmentElement, ILineHeightElement, ICornerElement
{
	/// <summary>The backing store for the <see cref="BorderColor" /> bindable property.</summary>
	public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(nameof(AvatarView.BorderColor), typeof(Color), typeof(IAvatarView), defaultValue: AvatarViewDefaults.DefaultBorderColor, propertyChanged: OnBorderColorPropertyChanged);

	/// <summary>The backing store for the <see cref="BorderWidth" /> bindable property.</summary>
	public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(nameof(AvatarView.BorderWidth), typeof(double), typeof(IAvatarView), defaultValue: AvatarViewDefaults.DefaultBorderWidth, propertyChanged: OnBorderWidthPropertyChanged);

	/// <summary>The backing store for the <see cref="CacheValidity" /> bindable property.</summary>
	public static readonly BindableProperty CacheValidityProperty = BindableProperty.Create(nameof(CacheValidity), typeof(TimeSpan), typeof(AvatarView), defaultValue: TimeSpan.FromDays(1));

	/// <summary>The backing store for the <see cref="CachingEnabled" /> bindable property.</summary>
	public static readonly BindableProperty CachingEnabledProperty = BindableProperty.Create(nameof(CachingEnabled), typeof(bool), typeof(AvatarView), defaultValue: true);

	/// <summary>The backing store for the <see cref="CornerRadius" /> bindable property.</summary>
	public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(nameof(AvatarView.CornerRadius), typeof(CornerRadius), typeof(ICornerElement), defaultValue: AvatarViewDefaults.DefaultCornerRadius, propertyChanged: OnCornerRadiusPropertyChanged);

	/// <summary>The backing store for the <see cref="DefaultGravatar" /> bindable property.</summary>
	public static readonly BindableProperty DefaultGravatarProperty = BindableProperty.Create(nameof(DefaultGravatar), typeof(DefaultGravatarImage), typeof(AvatarView), defaultValue: DefaultGravatarImage.MysteryPerson);

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

	readonly Image avatarImage = new()
	{
		Aspect = Aspect.AspectFill
	};

	readonly Label avatarLabel = new()
	{
		HorizontalTextAlignment = TextAlignment.Center,
		VerticalTextAlignment = TextAlignment.Center,
		Text = AvatarViewDefaults.DefaultText,
	};

	readonly string requestUriFormat = "https://www.gravatar.com/avatar/{0}?s={1}&d={2}";

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
	}

	/// <summary>Gets or sets the avatar font.</summary>
	public Microsoft.Maui.Font Font { get; set; } = Microsoft.Maui.Font.SystemFontOfSize((double)FontElement.FontSizeProperty.DefaultValue);

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

	/// <summary>Gets or sets a value of the control image cache validity property.</summary>
	public TimeSpan CacheValidity
	{
		get => (TimeSpan)GetValue(CacheValidityProperty);
		set => SetValue(CacheValidityProperty, value);
	}

	/// <summary>Gets or sets a value indicating whether the control image cache is enabled property.</summary>
	public bool CachingEnabled
	{
		get => (bool)GetValue(CachingEnabledProperty);
		set => SetValue(CachingEnabledProperty, value);
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

	/// <summary>Gets or sets a value of the control default gravatar property.</summary>
	public DefaultGravatarImage DefaultGravatar
	{
		get => (DefaultGravatarImage)GetValue(DefaultGravatarProperty);
		set => SetValue(DefaultGravatarProperty, value);
	}

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

	int IBorderElement.CornerRadius => (int)GetAverageCorderRadius(CornerRadius);

	int IBorderElement.CornerRadiusDefaultValue => (int)GetAverageCorderRadius((CornerRadius)CornerRadiusProperty.DefaultValue);

	Color IBorderElement.BorderColorDefaultValue => (Color)BorderColorProperty.DefaultValue;

	double IBorderElement.BorderWidthDefaultValue => (double)BorderWidthProperty.DefaultValue;

	static void OnCornerRadiusPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		AvatarView avatarView = (AvatarView)bindable;
		CornerRadius corderRadius = (CornerRadius)newValue;

		avatarView.StrokeShape = new RoundRectangle { CornerRadius = corderRadius };
	}

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

	static void OnImageSourceChanged(BindableObject bindable, object oldValue, object newValue)
	{
		AvatarView avatarView = (AvatarView)bindable;
		avatarView.HandleImageChanged((ImageSource?)newValue);
	}

	static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		AvatarView avatarView = (AvatarView)bindable;
		avatarView.avatarLabel.Text = (string)newValue;
	}

	static double GetAverageCorderRadius(in CornerRadius cornerRadius) =>
		new[] { cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomLeft, cornerRadius.BottomRight }.Average();

	void IBorderElement.OnBorderColorPropertyChanged(Color oldValue, Color newValue) => Stroke = newValue;

	bool IBorderElement.IsCornerRadiusSet() => IsSet(CornerRadiusProperty);

	bool IBorderElement.IsBackgroundColorSet() => IsSet(BackgroundColorProperty);

	bool IBorderElement.IsBackgroundSet() => IsSet(BackgroundProperty);

	bool IBorderElement.IsBorderColorSet() => IsSet(BorderColorProperty);

	bool IBorderElement.IsBorderWidthSet() => IsSet(BorderWidthProperty);

	double IFontElement.FontSizeDefaultValueCreator() => this.GetDefaultFontSize();

	void ITextElement.OnTextColorPropertyChanged(Color oldValue, Color newValue) => avatarLabel.TextColor = newValue;

	string ITextElement.UpdateFormsText(string original, TextTransform transform) => TextTransformUtilites.GetTransformedText(original, transform);

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

	void ITextElement.OnTextTransformChanged(TextTransform oldValue, TextTransform newValue)
	{
		InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
		avatarLabel.TextTransform = newValue;
	}

	void IImageSourcePart.UpdateIsLoading(bool isLoading)
	{
		if (!isLoading && wasImageLoading)
		{
			Handler?.UpdateValue(nameof(AvatarView));
		}

		wasImageLoading = isLoading;
	}

	void IImageElement.RaiseImageSourcePropertyChanged() => ((IImageElement)avatarImage).RaiseImageSourcePropertyChanged();

	void IImageElement.OnImageSourceSourceChanged(object sender, EventArgs e) =>
		((IImageElement)avatarImage).OnImageSourceSourceChanged(sender, e);

	void ITextAlignmentElement.OnHorizontalTextAlignmentPropertyChanged(TextAlignment oldValue, TextAlignment newValue) =>
		((ITextAlignmentElement)avatarLabel).OnHorizontalTextAlignmentPropertyChanged(oldValue, newValue);

	void ILineHeightElement.OnLineHeightChanged(double oldValue, double newValue) =>
		((ILineHeightElement)avatarLabel).OnLineHeightChanged(oldValue, newValue);

	void HandleFontChanged()
	{
		Handler?.UpdateValue(nameof(ITextStyle.Font));
		InvalidateMeasureInternal(InvalidationTrigger.MeasureChanged);
	}

	static string DefaultGravatarName(DefaultGravatarImage defaultGravatar)
			=> defaultGravatar switch
			{
				DefaultGravatarImage.FileNotFound => "404",
				DefaultGravatarImage.MysteryPerson => "mp",
				_ => $"{defaultGravatar}".ToLower(),
			};

	static string GetMd5Hash(string str)
	{
		using MD5 md5 = MD5.Create();
		byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
		StringBuilder sBuilder = new();
		foreach (byte hashByte in hash)
		{
			sBuilder.Append(hashByte.ToString("x2"));
		}
		return sBuilder.ToString();
	}

	static bool IsValidEmail(string email)
	{
		string trimmedEmail = email.Trim();
		if (trimmedEmail.EndsWith("."))
		{
			return false;
		}

		try
		{
			System.Net.Mail.MailAddress addr = new(email);
			return addr.Address == trimmedEmail;
		}
		catch
		{
			return false;
		}
	}

	void HandleImageChanged(ImageSource? newValue)
	{
		if (newValue is not null)
		{
			// Work-around for iOS / MacOS bug that paints `Border.BackgroundColor` over top of `Border.Content` when an Image is used for `Border.Content`
			if (OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsMacOS())
			{
				BackgroundColor = Colors.Transparent;
			}

			if (newValue is FileImageSource fileImageSource && IsValidEmail(fileImageSource.File))
			{
				double height = Height >= 0 ? Height : Math.Max(HeightRequest, AvatarViewDefaults.DefaultHeightRequest);
				double width = Width >= 0 ? Width : Math.Max(WidthRequest, AvatarViewDefaults.DefaultWidthRequest);
				int gravatarSize = (int)Math.Max(width, height);
				switch (gravatarSize) // Using Enumerable.Range is very inefficient for validating a number is within a range.  Much more efficient to simply use an if/switch.
				{
					case < 1:
						gravatarSize = 1;  // Images minimum is 1px
						break;
					case > 2048:
						gravatarSize = 2048; // Images maximum is 2048px, however note that many users have lower resolution images, so requesting larger sizes mar result in pixelation/low-quality images.
						break;
				}

				newValue = new UriImageSource()
				{
					Uri = new Uri(string.Format(requestUriFormat, GetMd5Hash(fileImageSource.File), gravatarSize, DefaultGravatarName(DefaultGravatar))),
				};
			}

			if (newValue is UriImageSource uriImageSource)
			{
				uriImageSource.CacheValidity = CacheValidity;
				uriImageSource.CachingEnabled = CachingEnabled;
				avatarImage.Source = uriImageSource;
			}
			else
			{
				avatarImage.Source = newValue;
			}

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
			double imageWidth = Width - (StrokeThickness * 2) - Padding.Left - Padding.Right;
			double imageHeight = Height - (StrokeThickness * 2) - Padding.Top - Padding.Bottom;
			Rect rect = new(0, 0, imageWidth, imageHeight);
			avatarImage.Clip = new RoundRectangleGeometry { CornerRadius = CornerRadius, Rect = rect };
		}
	}
}