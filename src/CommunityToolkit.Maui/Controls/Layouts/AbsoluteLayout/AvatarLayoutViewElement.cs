using System.Windows.Input;
using CommunityToolkit.Maui.Controls.Layouts.AbsoluteLayout;

namespace CommunityToolkit.Maui.Controls;

/// <summary>Avatar view element.</summary>
public static class AvatarLayoutViewElement
{
	/// <summary>Default corner radius.</summary>
	public const int DefaultCornerRadius = 50;

	/// <summary>Default height request.</summary>
	public const double DefaultHeightRequest = 100;

	/// <summary>Default avatar text.</summary>
	public const string DefaultText = "?";

	/// <summary>Default width request.</summary>
	public const double DefaultWidthRequest = 100;

	/// <summary>The backing store for the <see cref="IAvatarLayoutViewElement.AvatarBackgroundColor"/> bindable property.</summary>
	public static readonly BindableProperty AvatarBackgroundColorProperty = BindableProperty.Create(nameof(IAvatarLayoutViewElement.AvatarBackgroundColor), typeof(Color), typeof(VisualElement), defaultValue: (Color)VisualElement.BackgroundColorProperty.DefaultValue, propertyChanged: OnAvatarBackgroundColorPropertyChanged);

	/// <summary>The backing store for the <see cref="IAvatarLayoutViewElement.AvatarCornerRadius"/> bindable property.</summary>
	public static readonly BindableProperty AvatarCornerRadiusProperty = BindableProperty.Create(nameof(IAvatarLayoutViewElement.AvatarCornerRadius), typeof(int), typeof(IAvatarLayoutViewElement), defaultValue: BorderElement.DefaultCornerRadius, propertyChanged: OnAvatarCornerRadiusPropertyChanged);

	/// <summary>The backing store for the <see cref="IAvatarLayoutViewElement.AvatarHeightRequest" /> bindable property.</summary>
	public static readonly BindableProperty AvatarHeightRequestProperty = BindableProperty.Create(nameof(IAvatarLayoutViewElement.AvatarHeightRequest), typeof(double), typeof(IAvatarLayoutViewElement), defaultValue: DefaultHeightRequest, propertyChanged: OnAvatarHeightRequestPropertyChanged);

	/// <summary>The backing store for the <see cref="IAvatarLayoutViewElement.AvatarShadow" /> bindable property.</summary>
	public static readonly BindableProperty AvatarShadowProperty = BindableProperty.Create(nameof(IAvatarLayoutViewElement.AvatarShadow), typeof(Shadow), typeof(IAvatarLayoutViewElement), defaultValue: null, propertyChanged: OnAvatarShadowPropertyChanged);

	/// <summary>The backing store for the <see cref="IAvatarLayoutViewElement.AvatarWidthRequest" /> bindable property.</summary>
	public static readonly BindableProperty AvatarWidthRequestProperty = BindableProperty.Create(nameof(IAvatarLayoutViewElement.AvatarWidthRequest), typeof(double), typeof(IAvatarLayoutViewElement), defaultValue: DefaultWidthRequest, propertyChanged: OnAvatarWidthRequestPropertyChanged);

	/// <summary>The backing store for the <see cref="ImageSource" /> bindable property.</summary>
	public static readonly BindableProperty ImageSourceProperty = ImageElement.ImageSourceProperty;

	/// <summary>The backing store for the <see cref="IAvatarLayoutViewElement.Text" /> bindable property.</summary>
	public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(IAvatarLayoutViewElement.Text), typeof(string), typeof(IAvatarLayoutViewElement), defaultValue: DefaultText, propertyChanged: OnTextPropertyChanged);

	/// <summary>The backing store for the <see cref="IAvatarLayoutViewElement.Command" /> bindable property.</summary>
	public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(IAvatarLayoutViewElement.Command), typeof(ICommand), typeof(IAvatarLayoutViewElement), null, propertyChanging: OnCommandChanging, propertyChanged: OnCommandChanged);

	/// <summary>The backing store for the <see cref="IAvatarLayoutViewElement.CommandParameter" /> bindable property.</summary>
	public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(nameof(IAvatarLayoutViewElement.CommandParameter), typeof(object), typeof(IAvatarLayoutViewElement), null, propertyChanged: (bindable, _, __) => CommandCanExecuteChanged(bindable, EventArgs.Empty));

	internal static readonly BindablePropertyKey isPressedPropertyKey = BindableProperty.CreateReadOnly(nameof(IAvatarLayoutViewElement.IsPressed), typeof(bool), typeof(View), default(bool));

	/// <summary>The backing store for the <see cref="isPressedPropertyKey" /> bindable property.</summary>
	public static readonly BindableProperty IsPressedProperty = isPressedPropertyKey.BindableProperty;

	/// <summary>On avatar background colour changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnAvatarBackgroundColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarLayoutViewElement)bindable).OnAvatarBackgroundColorPropertyChanged((Color)oldValue, (Color)newValue);
	}

	/// <summary>On avatar corner radius property set.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnAvatarCornerRadiusPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarLayoutViewElement)bindable).OnAvatarCornerRadiusPropertyChanged((int)oldValue, (int)newValue);
	}

	/// <summary>On text property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnAvatarHeightRequestPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarLayoutViewElement)bindable).OnAvatarHeightRequestPropertyChanged((double)oldValue, (double)newValue);
	}

	/// <summary>On avatar height request property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnAvatarShadowPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarLayoutViewElement)bindable).OnAvatarShadowPropertyChanged((Shadow)oldValue, (Shadow)newValue);
	}

	/// <summary>On avatar width request property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnAvatarWidthRequestPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarLayoutViewElement)bindable).OnAvatarWidthRequestPropertyChanged((double)oldValue, (double)newValue);
	}

	/// <summary>On avatar shadow property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IAvatarLayoutViewElement)bindable).OnTextPropertyChanged((string)oldValue, (string)newValue);
	}

	/// <summary>On command property changed.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnCommandChanged(BindableObject bindable, object oldValue, object newValue)
	{
		IAvatarLayoutViewElement avatarLayoutView = (IAvatarLayoutViewElement)bindable;
		if (newValue is ICommand newCommand)
		{
			newCommand.CanExecuteChanged += avatarLayoutView.OnCommandCanExecuteChanged;
		}

		CommandChanged(avatarLayoutView);
	}

	/// <summary>On command property changing.</summary>
	/// <param name="bindable">Bindable object</param>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	public static void OnCommandChanging(BindableObject bindable, object oldValue, object newValue)
	{
		IAvatarLayoutViewElement avatarLayoutView = (IAvatarLayoutViewElement)bindable;
		if (oldValue is ICommand oldCommand)
		{
			oldCommand.CanExecuteChanged -= avatarLayoutView.OnCommandCanExecuteChanged;
		}
	}

	/// <summary>On command changed.</summary>
	/// <param name="sender">Avatar layout view element.</param>
	public static void CommandChanged(IAvatarLayoutViewElement sender)
	{
		if (sender.Command is not null)
		{
			CommandCanExecuteChanged(sender, EventArgs.Empty);
		}
		else
		{
			sender.IsEnabledCore = true;
		}
	}

	/// <summary>On command change execute changed.</summary>
	/// <param name="sender">Object sender.</param>
	/// <param name="e">Event arguments.</param>
	public static void CommandCanExecuteChanged(object sender, EventArgs e)
	{
		IAvatarLayoutViewElement avatarLayoutViewElementManager = (IAvatarLayoutViewElement)sender;
		ICommand cmd = avatarLayoutViewElementManager.Command;
		if (cmd is not null)
		{
			avatarLayoutViewElementManager.IsEnabledCore = cmd.CanExecute(avatarLayoutViewElementManager.CommandParameter);
		}
	}

	/// <summary>Visual element clicked.</summary>
	/// <param name="visualElement">Visual element.</param>
	/// <param name="avatarLayoutViewElementManager">Avatar layout view element manager.</param>
	public static void ElementClicked(VisualElement visualElement, IAvatarLayoutViewElement avatarLayoutViewElementManager)
	{
		if (visualElement.IsEnabled)
		{
			avatarLayoutViewElementManager.Command?.Execute(avatarLayoutViewElementManager.CommandParameter);
			avatarLayoutViewElementManager.PropagateUpClicked();
		}
	}

	/// <summary>Visual element pressed.</summary>
	/// <param name="visualElement">Visual element.</param>
	/// <param name="avatarLayoutViewElementManager">Avatar layout view element manager.</param>
	public static void ElementPressed(VisualElement visualElement, IAvatarLayoutViewElement avatarLayoutViewElementManager)
	{
		if (visualElement.IsEnabled)
		{
			avatarLayoutViewElementManager.SetIsPressed(true);
			visualElement.ChangeVisualStateInternal();
			avatarLayoutViewElementManager.PropagateUpPressed();
		}
	}

	/// <summary>Visual element released.</summary>
	/// <param name="visualElement">Visual element.</param>
	/// <param name="avatarLayoutViewElementManager">Avatar layout view element manager.</param>
	public static void ElementReleased(VisualElement visualElement, IAvatarLayoutViewElement avatarLayoutViewElementManager)
	{
		if (visualElement.IsEnabled)
		{
			avatarLayoutViewElementManager.SetIsPressed(false);
			visualElement.ChangeVisualStateInternal();
			avatarLayoutViewElementManager.PropagateUpReleased();
		}
	}
}