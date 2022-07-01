using Microsoft.Maui.Controls.Internals;

namespace CommunityToolkit.Maui.Controls;

/// <summary>Avatar content view element.</summary>
public static class AvatarElement
{
	/// <summary>Default height request.</summary>
	public const double DefaultHeightRequest = 100;

	/// <summary>Default avatar text.</summary>
	public const string DefaultText = "?";

	/// <summary>Default width request.</summary>
	public const double DefaultWidthRequest = 100;

	/// <summary>Default corner radius.</summary>
	public static readonly CornerRadius DefaultCornerRadius = new(50, 50, 50, 50);

	/// <summary>The backing store for the <see cref="IAvatarElement.AvatarBackgroundColor" /> bindable property.</summary>
	public static readonly BindableProperty AvatarBackgroundColorProperty = BindableProperty.Create(nameof(IAvatarElement.AvatarBackgroundColor), typeof(Color), typeof(IAvatarElement), null, propertyChanged: OnBackgroundColorChanged);

	/// <summary>The backing store for the <see cref="IAvatarElement.BorderWidth" /> bindable property.</summary>
	public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(nameof(IAvatarElement.BorderWidth), typeof(double), typeof(IAvatarElement), -1d, propertyChanged: OnBorderWidthPropertyChanged);

	/// <summary>The backing store for the <see cref="IAvatarElement.CornerRadius" /> bindable property.</summary>
	public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(nameof(IAvatarElement.CornerRadius), typeof(CornerRadius), typeof(ICornerElement), defaultValue: DefaultCornerRadius, propertyChanged: OnCornerRadiusPropertyChanged);

	/// <summary>The backing store for the <see cref="IFontElement.FontSize" /> bindable property.</summary>
	public static readonly BindableProperty FontSizeProperty = FontElement.FontSizeProperty;

	/// <summary>The backing store for the <see cref="ImageSource" /> bindable property.</summary>
	public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create("ImageSource", typeof(ImageSource), typeof(IAvatarElement), default(IAvatarElement), propertyChanged: OnImageSourceChanged);

	/// <summary>The backing store for the <see cref="IAvatarElement.Text" /> bindable property.</summary>
	public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(IAvatarElement.Text), typeof(string), typeof(IAvatarElement), defaultValue: DefaultText, propertyChanged: OnTextPropertyChanged);

	/// <summary>The backing store for the <see cref="IAvatarElement.AvatarWidthRequest" /> bindable property.</summary>
	public static readonly BindableProperty AvatarWidthRequestProperty = BindableProperty.Create(nameof(IAvatarElement.AvatarWidthRequest), typeof(double), typeof(VisualElement), -1d, propertyChanged: OnWidthRequestPropertyChanged);

	/// <summary>The backing store for the <see cref="IAvatarElement.AvatarHeightRequest" /> bindable property.</summary>
	public static readonly BindableProperty AvatarHeightRequestProperty = BindableProperty.Create(nameof(IAvatarElement.AvatarHeightRequest), typeof(double), typeof(VisualElement), -1d, propertyChanged: OnHeightRequestPropertyChanged);

	/// <summary>On background colour changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnBackgroundColorChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarElement)bindable).OnBackgroundColorChanged((Color)oldValue, (Color)newValue);
	}

	/// <summary>On border width property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnBorderWidthPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarElement)bindable).OnBorderWidthPropertyChanged((double)oldValue, (double)newValue);
	}

	/// <summary>On corner radius property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnCornerRadiusPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarElement)bindable).OnCornerRadiusPropertyChanged((CornerRadius)oldValue, (CornerRadius)newValue);
	}

	/// <summary>On image source changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnImageSourceChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarElement)bindable).OnImageSourceChanged((ImageSource)oldValue, (ImageSource)newValue);
	}

	/// <summary>On text property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarElement)bindable).OnTextPropertyChanged((string)oldValue, (string)newValue);
	}

	/// <summary>On width request property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnWidthRequestPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		OnRequestChanged(bindable);
		((IAvatarElement)bindable).OnWidthRequestPropertyChanged((double)oldValue, (double)newValue);
	}

	/// <summary>On height request property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnHeightRequestPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		OnRequestChanged(bindable);
		((IAvatarElement)bindable).OnHeightRequestPropertyChanged((double)oldValue, (double)newValue);
	}

	/// <summary>On visual element width or height request property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	public static void OnRequestChanged(BindableObject bindable)
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
}