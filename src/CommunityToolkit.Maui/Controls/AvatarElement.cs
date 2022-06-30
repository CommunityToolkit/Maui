namespace CommunityToolkit.Maui.Controls;

/// <summary>Avatar content view element.</summary>
public static class AvatarElement
{
	/// <summary>Default corner radius.</summary>
	public static readonly CornerRadius DefaultCornerRadius = new(50, 50, 50, 50);

	/// <summary>Default height request.</summary>
	public const double DefaultHeightRequest = 100;

	/// <summary>Default avatar text.</summary>
	public const string DefaultText = "?";

	/// <summary>Default width request.</summary>
	public const double DefaultWidthRequest = 100;

	/// <summary>The backing store for the <see cref="IAvatarElement.Text" /> bindable property.</summary>
	public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(IAvatarElement.Text), typeof(string), typeof(IAvatarElement), defaultValue: DefaultText, propertyChanged: OnTextPropertyChanged);

	/// <summary>The backing store for the <see cref="IAvatarElement.CornerRadius" /> bindable property.</summary>
	public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(nameof(IAvatarElement.CornerRadius), typeof(CornerRadius), typeof(ICornerElement), defaultValue: DefaultCornerRadius, propertyChanged: OnCornerRadiusPropertyChanged);

	/// <summary>The backing store for the <see cref="ImageSource" /> bindable property.</summary>
	public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create("ImageSource", typeof(ImageSource), typeof(IImageElement), default(ImageSource), propertyChanged: OnImageSourceChanged);

	/// <summary>On text property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarElement)bindable).OnTextPropertyChanged((string)oldValue, (string)newValue);
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
	static void OnImageSourceChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarElement)bindable).OnImageSourceChanged((ImageSource)oldValue, (ImageSource)newValue);
	}
}