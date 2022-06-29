using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Controls.Layouts.AbsoluteLayout;
using Microsoft.Maui.Controls.Internals;

namespace CommunityToolkit.Maui.Controls;

/// <summary>Avatar view.</summary>
public partial class AvatarLayoutView : AbsoluteLayout, IElementConfiguration<AvatarLayoutView>, IFontElement, ITextElement, IAvatarLayoutViewElement, IImageElement
{
	#region Constructor
	/// <summary>Initialises a new instance of the <see cref="AvatarLayoutView"/> class.</summary>
	/// <remarks>Builds the composite control.</remarks>
	public AvatarLayoutView()
	{
		platformConfigurationRegistry = new Lazy<PlatformConfigurationRegistry<AvatarLayoutView>>(() => new PlatformConfigurationRegistry<AvatarLayoutView>(this));
		grid.Add(label);
		grid.Add(image);
		frame.Content = grid;
		Children.Add(frame);
		WidthRequest = AvatarLayoutViewElement.DefaultWidthRequest;
		HeightRequest = AvatarLayoutViewElement.DefaultHeightRequest;
		IsEnabled = true;
		HandleImageChanged();
	}

	Frame frame { get; } = new Frame
	{
		Margin = 0,
		Padding = 0,
		HorizontalOptions = LayoutOptions.Center,
		VerticalOptions = LayoutOptions.Center,
		IsClippedToBounds = true,
		CornerRadius = AvatarLayoutViewElement.DefaultCornerRadius,
		WidthRequest = AvatarLayoutViewElement.DefaultWidthRequest,
		HeightRequest = AvatarLayoutViewElement.DefaultHeightRequest,
	};

	Grid grid { get; } = new Grid();

	Label label { get; } = new Label
	{
		HorizontalTextAlignment = TextAlignment.Center,
		VerticalTextAlignment = TextAlignment.Center,
		IsVisible = true,
		Text = AvatarLayoutViewElement.DefaultText,
	};

	Image image { get; } = new Image
	{
		IsVisible = false,
		Aspect = Aspect.AspectFill,
	};

	readonly Lazy<PlatformConfigurationRegistry<AvatarLayoutView>> platformConfigurationRegistry;

	/// <summary>Event handler for clicked.</summary>
	public event EventHandler? Clicked;

	/// <summary>Event handler for pressed.</summary>
	public event EventHandler? Pressed;

	/// <summary>Event handler for released.</summary>
	public event EventHandler? Released;

	#endregion

	#region Text element

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

	/// <inheritdoc/>
	public string UpdateFormsText(string original, TextTransform transform)
	{
		return TextTransformUtilites.GetTransformedText(original, transform);
	}

	#endregion

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
	#endregion

	#region Image Element
	Aspect IImageElement.Aspect => Aspect.AspectFit;
	ImageSource IImageElement.Source => ImageSource;
	bool IImageElement.IsOpaque => false;

	bool IImageElement.IsLoading => false;

	bool IImageElement.IsAnimationPlaying => false;

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
	[TypeConverter(typeof(ImageSourceConverter))]
	public ImageSource ImageSource
	{
		get => (ImageSource)GetValue(ImageElement.ImageSourceProperty);
		set => SetValue(ImageElement.ImageSourceProperty, value);
	}

	/// <inheritdoc/>
	protected override void OnBindingContextChanged()
	{
		ImageSource bindingImage = ImageSource;
		if (bindingImage is not null)
		{
			SetInheritedBindingContext(bindingImage, BindingContext);
		}

		base.OnBindingContextChanged();
		HandleImageChanged();
	}

	void HandleImageChanged()
	{
		if (ImageSource is not null)
		{
			label.IsVisible = false;
			image.Source = ImageSource;
			image.IsVisible = true;
		}
		else
		{
			label.IsVisible = true;
			image.IsVisible = false;
		}
	}

	#endregion

	#region AvatarView element

	bool IAvatarLayoutViewElement.IsTextSet()
	{
		return IsSet(AvatarLayoutViewElement.TextProperty);
	}

	bool IAvatarLayoutViewElement.IsAvatarBackgroundColorSet()
	{
		return IsSet(AvatarLayoutViewElement.AvatarBackgroundColorProperty);
	}

	bool IAvatarLayoutViewElement.IsAvatarCornerRadiusSet()
	{
		return IsSet(AvatarLayoutViewElement.AvatarCornerRadiusProperty);
	}

	bool IAvatarLayoutViewElement.IsAvatarHeightRequestSet()
	{
		return IsSet(AvatarLayoutViewElement.AvatarHeightRequestProperty);
	}

	bool IAvatarLayoutViewElement.IsAvatarShadowSet()
	{
		return IsSet(AvatarLayoutViewElement.AvatarShadowProperty);
	}

	bool IAvatarLayoutViewElement.IsAvatarWidthRequestSet()
	{
		return IsSet(AvatarLayoutViewElement.AvatarWidthRequestProperty);
	}

	string IAvatarLayoutViewElement.TextDefaultValue => (string)AvatarLayoutViewElement.TextProperty.DefaultValue;
	int IAvatarLayoutViewElement.AvatarCorderRadiusDefault => (int)AvatarLayoutViewElement.AvatarCornerRadiusProperty.DefaultValue;
	double IAvatarLayoutViewElement.AvatarHeightRequestDefault => (double)AvatarLayoutViewElement.AvatarHeightRequestProperty.DefaultValue;
	double IAvatarLayoutViewElement.AvatarWidthRequestDefault => (double)AvatarLayoutViewElement.AvatarWidthRequestProperty.DefaultValue;
	Shadow IAvatarLayoutViewElement.AvatarShadowDefault => (Shadow)AvatarLayoutViewElement.AvatarShadowProperty.DefaultValue;
	Color IAvatarLayoutViewElement.AvatarBackgroundColorDefault => (Color)AvatarLayoutViewElement.AvatarBackgroundColorProperty.DefaultValue;

	void IAvatarLayoutViewElement.OnTextPropertyChanged(string oldValue, string newValue)
	{
		label.Text = newValue;
	}

	void IAvatarLayoutViewElement.OnAvatarBackgroundColorPropertyChanged(Color oldValue, Color newValue)
	{
		frame.BackgroundColor = newValue;
	}

	void IAvatarLayoutViewElement.OnAvatarCornerRadiusPropertyChanged(int oldValue, int newValue)
	{
		frame.CornerRadius = newValue;
	}

	void IAvatarLayoutViewElement.OnAvatarHeightRequestPropertyChanged(double oldValue, double newValue)
	{
		HeightRequest = newValue;
		frame.HeightRequest = newValue;
	}

	void IAvatarLayoutViewElement.OnAvatarShadowPropertyChanged(Shadow oldValue, Shadow newValue)
	{
		frame.Shadow = newValue;
	}

	void IAvatarLayoutViewElement.OnAvatarWidthRequestPropertyChanged(double oldValue, double newValue)
	{
		WidthRequest = newValue;
		frame.WidthRequest = newValue;
	}

	/// <inheritdoc/>
	public IPlatformElementConfiguration<T, AvatarLayoutView> On<T>() where T : IConfigPlatform
	{
		return platformConfigurationRegistry.Value.On<T>();
	}

	/// <summary>Gets or sets the value of the avatar background colour property.</summary>
	public Color AvatarBackgroundColor
	{
		get => (Color)GetValue(AvatarLayoutViewElement.AvatarBackgroundColorProperty);
		set => SetValue(AvatarLayoutViewElement.AvatarBackgroundColorProperty, value);
	}

	/// <summary>Gets or sets the value of the avatar corner radius property.</summary>
	public int AvatarCornerRadius
	{
		get => (int)GetValue(AvatarLayoutViewElement.AvatarCornerRadiusProperty);
		set => SetValue(AvatarLayoutViewElement.AvatarCornerRadiusProperty, value);
	}

	/// <summary>Gets or sets the value of the avatar height request property.</summary>
	public double AvatarHeightRequest
	{
		get => (double)GetValue(AvatarLayoutViewElement.AvatarHeightRequestProperty);
		set => SetValue(AvatarLayoutViewElement.AvatarHeightRequestProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar shadow property.</summary>
	public Shadow AvatarShadow
	{
		get => (Shadow)GetValue(AvatarLayoutViewElement.AvatarShadowProperty);
		set => SetValue(AvatarLayoutViewElement.AvatarShadowProperty, value);
	}

	/// <summary>Gets or sets a valid of the avatar width request property.</summary>
	public double AvatarWidthRequest
	{
		get => (double)GetValue(AvatarLayoutViewElement.AvatarWidthRequestProperty);
		set => SetValue(AvatarLayoutViewElement.AvatarWidthRequestProperty, value);
	}

	/// <summary>Gets or sets a value of the avatar text property.</summary>
	public string Text
	{
		get => (string)GetValue(AvatarLayoutViewElement.TextProperty);
		set => SetValue(AvatarLayoutViewElement.TextProperty, value);
	}

	/// <summary>Gets a value indicating the element is pressed.</summary>
	public bool IsPressed => (bool)GetValue(AvatarLayoutViewElement.IsPressedProperty);

	/// <summary>Gets or sets a value indicating the command property.</summary>
	public ICommand Command
	{
		get => (ICommand)GetValue(AvatarLayoutViewElement.CommandProperty);
		set => SetValue(AvatarLayoutViewElement.CommandProperty, value);
	}

	/// <summary>Gets or sets a value indicating the command parameter property.</summary>
	public object CommandParameter
	{
		get => GetValue(AvatarLayoutViewElement.CommandParameterProperty);
		set => SetValue(AvatarLayoutViewElement.CommandParameterProperty, value);
	}

	/// <summary>Sets a value indicating whether the element is enabled property.</summary>
	bool IAvatarLayoutViewElement.IsEnabledCore
	{
		set => SetValueCore(IsEnabledProperty, value);
	}

	/// <summary>Set is pressed.</summary>
	/// <param name="isPressed">True if pressed.</param>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void SetIsPressed(bool isPressed)
	{
		SetValue(AvatarLayoutViewElement.isPressedPropertyKey, isPressed);
	}

	/// <summary>Send clicked.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void SendClicked()
	{
		AvatarLayoutViewElement.ElementClicked(this, this);
	}

	/// <summary>Send pressed.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void SendPressed()
	{
		AvatarLayoutViewElement.ElementPressed(this, this);
	}

	/// <summary>Send released.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void SendReleased()
	{
		AvatarLayoutViewElement.ElementReleased(this, this);
	}

	/// <summary>Propagate up clicked.</summary>
	public void PropagateUpClicked()
	{
		Clicked?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>Propagate up pressed.</summary>
	public void PropagateUpPressed()
	{
		Pressed?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>Propagate up released.</summary>
	public void PropagateUpReleased()
	{
		Released?.Invoke(this, EventArgs.Empty);
	}

	void IAvatarLayoutViewElement.OnCommandCanExecuteChanged(object? sender, EventArgs e)
	{
		AvatarLayoutViewElement.CommandCanExecuteChanged(this, EventArgs.Empty);
	}

	#endregion
}