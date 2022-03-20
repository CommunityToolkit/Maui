using System.ComponentModel;
using System.Windows.Input;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="UserStoppedTypingBehavior"/> is a behavior that allows the user to trigger an action when a user has stopped data input any <see cref="InputView"/> derivate like <see cref="Entry"/> or <see cref="SearchBar"/>. Examples of its usage include triggering a search when a user has stopped entering their search query.
/// </summary>
public class UserStoppedTypingBehavior : BaseBehavior<InputView>
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="Command"/> property.
	/// </summary>
	public static readonly BindableProperty CommandProperty
		= BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(UserStoppedTypingBehavior));

	/// <summary>
	/// Backing BindableProperty for the <see cref="CommandParameter"/> property.
	/// </summary>
	public static readonly BindableProperty CommandParameterProperty
		= BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(UserStoppedTypingBehavior));

	/// <summary>
	/// Backing BindableProperty for the <see cref="StoppedTypingTimeThreshold"/> property.
	/// </summary>
	public static readonly BindableProperty StoppedTypingTimeThresholdProperty
		= BindableProperty.Create(nameof(StoppedTypingTimeThreshold), typeof(int), typeof(UserStoppedTypingBehavior), 1000);

	/// <summary>
	/// Backing BindableProperty for the <see cref="MinimumLengthThreshold"/> property.
	/// </summary>
	public static readonly BindableProperty MinimumLengthThresholdProperty
		= BindableProperty.Create(nameof(MinimumLengthThreshold), typeof(int), typeof(UserStoppedTypingBehavior), 0);

	/// <summary>
	/// Backing BindableProperty for the <see cref="ShouldDismissKeyboardAutomatically"/> property.
	/// </summary>
	public static readonly BindableProperty ShouldDismissKeyboardAutomaticallyProperty
		= BindableProperty.Create(nameof(ShouldDismissKeyboardAutomatically), typeof(bool), typeof(UserStoppedTypingBehavior), false);

	CancellationTokenSource? tokenSource;

	/// <summary>
	/// Command that is triggered when the <see cref="StoppedTypingTimeThreshold" /> is reached. When <see cref="MinimumLengthThreshold"/> is set, it's only triggered when both conditions are met. This is a bindable property.
	/// </summary>
	public ICommand? Command
	{
		get => (ICommand?)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	/// <summary>
	/// An optional parameter to forward to the <see cref="Command"/>. This is a bindable property.
	/// </summary>
	public object? CommandParameter
	{
		get => GetValue(CommandParameterProperty);
		set => SetValue(CommandParameterProperty, value);
	}

	/// <summary>
	/// The time of inactivity in milliseconds after which <see cref="Command"/> will be executed. If <see cref="MinimumLengthThreshold"/> is also set, the condition there also needs to be met. This is a bindable property.
	/// </summary>
	public int StoppedTypingTimeThreshold
	{
		get => (int)GetValue(StoppedTypingTimeThresholdProperty);
		set => SetValue(StoppedTypingTimeThresholdProperty, value);
	}

	/// <summary>
	/// The minimum length of the input value required before <see cref="Command"/> will be executed but only after <see cref="StoppedTypingTimeThreshold"/> has passed. This is a bindable property.
	/// </summary>
	public int MinimumLengthThreshold
	{
		get => (int)GetValue(MinimumLengthThresholdProperty);
		set => SetValue(MinimumLengthThresholdProperty, value);
	}

	/// <summary>
	/// Indicates whether or not the keyboard should be dismissed automatically after the user stopped typing. This is a bindable property.
	/// </summary>
	public bool ShouldDismissKeyboardAutomatically
	{
		get => (bool)GetValue(ShouldDismissKeyboardAutomaticallyProperty);
		set => SetValue(ShouldDismissKeyboardAutomaticallyProperty, value);
	}

	/// <inheritdoc/>
	protected override void OnViewPropertyChanged(InputView sender, PropertyChangedEventArgs e)
	{
		base.OnViewPropertyChanged(sender, e);

		if (e.PropertyName == InputView.TextProperty.PropertyName)
		{
			OnTextPropertyChanged();
		}
	}

	void OnTextPropertyChanged()
	{
		if (tokenSource != null)
		{
			tokenSource.Cancel();
			tokenSource.Dispose();
		}
		tokenSource = new CancellationTokenSource();

		Task.Delay(StoppedTypingTimeThreshold, tokenSource.Token)
			.ContinueWith(task =>
			{
				if (task.IsFaulted && task.Exception != null)
				{
					throw task.Exception;
				}

				if (task.Status == TaskStatus.Canceled ||
					View?.Text?.Length < MinimumLengthThreshold)
				{
					return;
				}

				if (View != null && ShouldDismissKeyboardAutomatically)
				{
					Dispatcher.DispatchIfRequired(View.Unfocus);
				}

				if (View != null && Command?.CanExecute(CommandParameter ?? View.Text) is true)
				{
					Command.Execute(CommandParameter ?? View.Text);
				}
			});
	}
}