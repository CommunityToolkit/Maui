using System.ComponentModel;
using CommunityToolkit.Maui.Core.Views.BadgeView;
using Microsoft.Maui.Controls.Internals;
using Geometry = Microsoft.Maui.Controls.Shapes.Geometry;
using TypeConverterAttribute = System.ComponentModel.TypeConverterAttribute;

namespace CommunityToolkit.Maui.Views.BadgeView;

/// <summary>
/// The <see cref="BadgeView"/> allows the user to show a badge with a string value on top of any control. By wrapping a control in a <see cref="BadgeView"/> control, you can show a badge value on top of it. This is very much like the badges you see on the app icons on iOS and Android.
/// </summary>
[ContentProperty(nameof(Content))]
public class BadgeView : BaseTemplatedView<Grid>, IBadgeView
{
	bool isVisible;
	bool placementDone;

	/// <summary>
	/// Backing BindableProperty for the <see cref="Content"/> property.
	/// </summary>
	public static readonly BindableProperty ContentProperty =
		BindableProperty.Create(nameof(Content), typeof(View), typeof(BadgeView),
			propertyChanged: OnLayoutPropertyChanged);

	static void OnLayoutPropertyChanged(BindableObject bindable, object oldValue, object newValue) => (bindable as BadgeView)?.UpdateLayout();

	/// <summary>
	/// Gets or sets the <see cref="View"/> on top of which the <see cref="BadgeView"/> will be shown. This is a bindable property.
	/// </summary>
	public View? Content
	{
		get => (View?)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="BadgePosition"/> property.
	/// </summary>
	public static readonly BindableProperty BadgePositionProperty =
		BindableProperty.Create(nameof(BadgePosition), typeof(BadgePosition), typeof(BadgeView), BadgePosition.TopRight,
			propertyChanged: OnBadgePositionChanged);

	/// <summary>
	/// Determines the position where the badge will be shown on top of <see cref="Content"/>. This is a bindable property.
	/// </summary>
	public BadgePosition BadgePosition
	{
		get => (BadgePosition)GetValue(BadgePositionProperty);
		set => SetValue(BadgePositionProperty, value);
	}

	static void OnBadgePositionChanged(BindableObject bindable, object oldValue, object newValue) => (bindable as BadgeView)?.UpdateBadgeViewPlacement(true);

	/// <summary>
	/// Backing BindableProperty for the <see cref="AutoHide"/> property.
	/// </summary>
	public static BindableProperty AutoHideProperty =
		BindableProperty.Create(nameof(AutoHide), typeof(bool), typeof(BadgeView), defaultValue: true,
			propertyChanged: OnAutoHideChanged);

	/// <summary>
	/// Determines whether or not the badge is automatically hidden when the <see cref="Text"/> is set to 0. This is a bindable property.
	/// </summary>
	public bool AutoHide
	{
		get => (bool)GetValue(AutoHideProperty);
		set => SetValue(AutoHideProperty, value);
	}

	static async void OnAutoHideChanged(BindableObject bindable, object oldValue, object newValue) => await ((BadgeView)bindable).UpdateVisibilityAsync();

	/// <summary>
	/// Backing BindableProperty for the <see cref="IsAnimated"/> property.
	/// </summary>
	public static BindableProperty IsAnimatedProperty =
		BindableProperty.Create(nameof(IsAnimated), typeof(bool), typeof(BadgeView), defaultValue: true);

	/// <summary>
	/// Determines if an animation is used when the badge is shown or hidden. This is a bindable property.
	/// </summary>
	public bool IsAnimated
	{
		get => (bool)GetValue(IsAnimatedProperty);
		set => SetValue(IsAnimatedProperty, value);
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="BadgeAnimation"/> property.
	/// </summary>
	public static BindableProperty BadgeAnimationProperty =
		BindableProperty.Create(nameof(BadgeAnimation), typeof(IBadgeAnimation), typeof(BadgeView), new BadgeAnimation());

	/// <summary>
	/// Gets or sets the animation that is used when the badge is shown or hidden. The animation only shows when <see cref="IsAnimated"/> is set to true. This is a bindable property.
	/// </summary>
	public IBadgeAnimation? BadgeAnimation
	{
		get => (IBadgeAnimation?)GetValue(BadgeAnimationProperty);
		set => SetValue(BadgeAnimationProperty, value);
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="BackgroundColor"/> property.
	/// </summary>
	public static new BindableProperty BackgroundColorProperty =
		BindableProperty.Create(nameof(BackgroundColor), typeof(Color), typeof(BadgeView), defaultValue: null,
			propertyChanged: OnLayoutPropertyChanged);

	/// <summary>
	/// Gets or sets the background <see cref="Color"/> of the badge. This is a bindable property.
	/// </summary>
	public new Color BackgroundColor
	{
		get => (Color)GetValue(BackgroundColorProperty);
		set => SetValue(BackgroundColorProperty, value);
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="BorderColor"/> property.
	/// </summary>
	public static readonly BindableProperty BorderColorProperty =
		BindableProperty.Create(nameof(BorderColor), typeof(Color), typeof(BadgeView), null,
			propertyChanged: OnLayoutPropertyChanged);

	/// <summary>
	/// Gets or sets the border <see cref="Color"/> of the badge. This is a bindable property.
	/// </summary>
	public Color BorderColor
	{
		get => (Color)GetValue(BorderColorProperty);
		set => SetValue(BorderColorProperty, value);
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="HasShadow"/> property.
	/// </summary>
	public static readonly BindableProperty HasShadowProperty =
		BindableProperty.Create(nameof(HasShadow), typeof(bool), typeof(BadgeView), false,
			propertyChanged: OnLayoutPropertyChanged);

	/// <summary>
	/// Enabled or disables a shadow being shown behind the <see cref="BadgeView"/>. This is a bindable property.
	/// </summary>
	public bool HasShadow
	{
		get => (bool)GetValue(HasShadowProperty);
		set => SetValue(HasShadowProperty, value);
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="TextColor"/> property.
	/// </summary>
	public static BindableProperty TextColorProperty =
		BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(BadgeView), defaultValue: null,
			propertyChanged: OnLayoutPropertyChanged);

	/// <summary>
	/// Gets or sets the <see cref="Color"/> or the test shown in  the <see cref="BadgeView"/>. This is a bindable property.
	/// </summary>
	public Color TextColor
	{
		get => (Color)GetValue(TextColorProperty);
		set => SetValue(TextColorProperty, value);
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="Text"/> property.
	/// </summary>
	public static BindableProperty TextProperty =
		BindableProperty.Create(nameof(Text), typeof(string), typeof(BadgeView), defaultValue: "0",
			propertyChanged: OnTextChanged);

	/// <summary>
	/// Text that is shown on the <see cref="BadgeView"/>. Set this property to 0 and <see cref="AutoHide"/> to true, to make the badge disappear automatically. This is a bindable property.
	/// </summary>
	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	static async void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is BadgeView badgeView)
		{
			badgeView.UpdateLayout();
			await badgeView.UpdateVisibilityAsync();
		}
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="FontSize"/> property.
	/// </summary>
	public static BindableProperty FontSizeProperty =
		BindableProperty.Create(nameof(FontSize), typeof(double), typeof(BadgeView), 10.0d,
			propertyChanged: OnFontChanged);

	static void OnFontChanged(BindableObject bindable, object oldValue, object newValue) => (bindable as BadgeView)?.UpdateFont();

	/// <summary>
	/// Font size of all the text on the <see cref="BadgeView" />. <see cref="NamedSize" /> values can be used. This is a bindable property.
	/// </summary>
	[TypeConverter(typeof(FontSizeConverter))]
	public double FontSize
	{
		get => (double)GetValue(FontSizeProperty);
		set => SetValue(FontSizeProperty, value);
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="FontFamily"/> property.
	/// </summary>
	public static BindableProperty FontFamilyProperty =
		BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(BadgeView), string.Empty,
			propertyChanged: OnFontChanged);

	/// <summary>
	/// Font of the text on the <see cref="BadgeView" />. This is a bindable property.
	/// </summary>
	public string FontFamily
	{
		get => (string)GetValue(FontFamilyProperty);
		set => SetValue(FontFamilyProperty, value);
	}

	/// <summary>
	/// Backing BindableProperty for the <see cref="FontAttributes"/> property.
	/// </summary>
	public static BindableProperty FontAttributesProperty =
		BindableProperty.Create(nameof(FontAttributes), typeof(FontAttributes), typeof(BadgeView), FontAttributes.None,
			propertyChanged: OnFontChanged);

	/// <summary>
	/// Font attributes of all the text on the <see cref="BadgeView" />. This is a bindable property.
	/// </summary>
	public FontAttributes FontAttributes
	{
		get => (FontAttributes)GetValue(FontAttributesProperty);
		set => SetValue(FontAttributesProperty, value);
	}

	ContentPresenter BadgeContent { get; } = CreateContentElement();

	Grid BadgeIndicatorContainer { get; } = CreateIndicatorContainerElement();

	Frame BadgeIndicatorBackground { get; } = CreateIndicatorBackgroundElement();

	Label BadgeText { get; } = CreateTextElement();

	/// <inheritdoc />
	protected override void OnControlInitialized(Grid control)
	{
		BadgeIndicatorBackground.Content = BadgeText;

		BadgeIndicatorContainer.Children.Add(BadgeIndicatorBackground);
		BadgeIndicatorContainer.PropertyChanged += OnBadgeIndicatorContainerPropertyChanged;
		BadgeText.SizeChanged += OnBadgeTextSizeChanged;

		control.Children.Add(BadgeContent);
		control.Children.Add(BadgeIndicatorContainer);
	}

	static ContentPresenter CreateContentElement() => new ContentPresenter
	{
		HorizontalOptions = LayoutOptions.Start,
		VerticalOptions = LayoutOptions.Start
	};

	static Grid CreateIndicatorContainerElement() => new Grid
	{
		HorizontalOptions = LayoutOptions.Start,
		VerticalOptions = LayoutOptions.Start,
		IsVisible = false
	};

	static Frame CreateIndicatorBackgroundElement() => new Frame
	{
		CornerRadius = Device.RuntimePlatform == Device.Android ? 12 : 8,
		Padding = 2
	};

	static Label CreateTextElement() => new Label
	{
		HorizontalOptions = LayoutOptions.Center,
		VerticalOptions = LayoutOptions.Center,
		Margin = new Thickness(4, 0)
	};

	/// <inheritdoc />
	protected override void OnBindingContextChanged()
	{
		base.OnBindingContextChanged();

		SetInheritedBindingContext(Content, BindingContext);
	}

	/// <inheritdoc />
	protected override void OnSizeAllocated(double width, double height)
	{
		base.OnSizeAllocated(width, height);

		UpdateBadgeViewPlacement();
	}

	void UpdateLayout()
	{
		BatchBegin();
		BadgeContent.BatchBegin();
		BadgeIndicatorContainer.BatchBegin();
		BadgeIndicatorBackground.BatchBegin();
		BadgeText.BatchBegin();

		BadgeContent.Content = Content;

		BadgeIndicatorBackground.BackgroundColor = BackgroundColor;
		BadgeIndicatorBackground.BorderColor = BorderColor;
		BadgeIndicatorBackground.HasShadow = HasShadow;

		BadgeText.Text = Text;
		BadgeText.TextColor = TextColor;

		BadgeContent.BatchCommit();
		BadgeIndicatorContainer.BatchCommit();
		BadgeIndicatorBackground.BatchCommit();
		BadgeText.BatchCommit();
		BatchCommit();
	}

	void UpdateFont()
	{
		if (BadgeText is null)
		{
			return;
		}

		BadgeText.FontSize = FontSize;
		BadgeText.FontFamily = FontFamily;
		BadgeText.FontAttributes = FontAttributes;
	}

	void UpdateBadgeViewPlacement(bool force = false)
	{
		if (BadgeContent.Height <= 0 && BadgeContent.Width <= 0)
		{
			return;
		}

		if (force)
		{
			placementDone = false;
		}

		if (placementDone)
		{
			return;
		}

		var containerMargin = new Thickness(0);
		var contentMargin = new Thickness(0);

		if (BadgeIndicatorContainer.IsVisible)
		{
			const double Padding = 6;
			var size = Math.Max(BadgeText.Height, BadgeText.Width) + Padding;
			BadgeIndicatorBackground.HeightRequest = size;
			var margins = GetMargins(size);
			containerMargin = margins.ContainerMargin;
			contentMargin = margins.ContentMargin;
		}

		BadgeIndicatorContainer.Margin = containerMargin;
		BadgeContent.Margin = contentMargin;
		placementDone = true;
	}

	(Thickness ContainerMargin, Thickness ContentMargin) GetMargins(double size)
	{
		double verticalMargin;
		double horizontalMargin;
		var containerMargin = new Thickness(0);
		var contentMargin = new Thickness(0);
		switch (BadgePosition)
		{
			case BadgePosition.TopRight:
				verticalMargin = size / 2;
				horizontalMargin = BadgeContent.Width - verticalMargin;
				containerMargin = new Thickness(horizontalMargin, 0, 0, 0);
				contentMargin = new Thickness(0, verticalMargin, verticalMargin, 0);
				break;

			case BadgePosition.TopLeft:
				verticalMargin = size / 2;
				containerMargin = new Thickness(0, 0, 0, 0);
				contentMargin = new Thickness(verticalMargin, verticalMargin, 0, 0);
				break;

			case BadgePosition.BottomLeft:
				verticalMargin = size / 2;
				var bottomLeftverticalMargin = BadgeContent.Height - verticalMargin;
				containerMargin = new Thickness(0, bottomLeftverticalMargin, 0, 0);
				contentMargin = new Thickness(verticalMargin, 0, 0, 0);
				break;

			case BadgePosition.BottomRight:
				verticalMargin = size / 2;
				var bottomRightverticalMargin = BadgeContent.Height - verticalMargin;
				horizontalMargin = BadgeContent.Width - verticalMargin;
				containerMargin = new Thickness(horizontalMargin, bottomRightverticalMargin, 0, 0);
				contentMargin = new Thickness(0, 0, verticalMargin, 0);
				break;
		}
		return (containerMargin, contentMargin);
	}

	async Task UpdateVisibilityAsync()
	{
		if (BadgeIndicatorBackground is null)
		{
			return;
		}

		var badgeText = BadgeText.Text;

		if (string.IsNullOrEmpty(badgeText))
		{
			BadgeIndicatorBackground.IsVisible = false;
			return;
		}

		var badgeIsVisible = !AutoHide || !badgeText.Trim().Equals("0");
		BadgeIndicatorBackground.IsVisible = badgeIsVisible;

		if (IsAnimated)
		{
			if (badgeIsVisible == isVisible)
			{
				return;
			}

			if (badgeIsVisible)
			{
				BadgeIndicatorContainer.IsVisible = true;

				if (BadgeAnimation != null)
				{
					await BadgeAnimation.OnAppearing(BadgeIndicatorContainer);
				}
			}
			else
			{
				if (BadgeAnimation != null)
				{
					await BadgeAnimation.OnDisappearing(BadgeIndicatorContainer);
				}

				BadgeIndicatorContainer.IsVisible = false;
			}

			isVisible = badgeIsVisible;
		}
		else
		{
			BadgeIndicatorContainer.IsVisible = badgeIsVisible;
		}
	}

	void OnBadgeTextSizeChanged(object? sender, EventArgs e)
		=> UpdateBadgeViewPlacement(true);

	void OnBadgeIndicatorContainerPropertyChanged(object? sender, PropertyChangedEventArgs e)
		=> UpdateBadgeViewPlacement(true);
}