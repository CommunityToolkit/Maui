using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="MaxLengthReachedBehavior"/> is a behavior that allows the user to trigger an action when a user has reached the maximum length allowed on an <see cref="InputView"/>. It can either trigger a <see cref="ICommand"/> or an event depending on the user's preferred scenario.
/// </summary>
public partial class MaxLengthReachedBehavior : BaseBehavior<InputView>
{
	readonly WeakEventManager maxLengthReachedEventManager = new();

	/// <summary>
	/// Command that is triggered when the value configured in <see cref="InputView.MaxLength" /> is reached. Both the <see cref="MaxLengthReached"/> event and this command are triggered. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial ICommand? Command { get; set; } = MaxLengthReachedBehaviorDefaults.Command;

	/// <summary>
	/// Indicates whether the keyboard should be dismissed automatically after the maximum length is reached. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial bool ShouldDismissKeyboardAutomatically { get; set; } = MaxLengthReachedBehaviorDefaults.ShouldDismissKeyboardAutomatically;

	/// <summary>
	/// Event that is triggered when the value configured in <see cref="InputView.MaxLength" /> is reached. Both the <see cref="Command"/> and this event are triggered. This is a bindable property.
	/// </summary>
	public event EventHandler<MaxLengthReachedEventArgs> MaxLengthReached
	{
		add => maxLengthReachedEventManager.AddEventHandler(value);
		remove => maxLengthReachedEventManager.RemoveEventHandler(value);
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
		if (View?.Text == null || View.Text.Length < View.MaxLength)
		{
			return;
		}

		if (ShouldDismissKeyboardAutomatically)
		{
			View.Unfocus();
		}

		var newTextValue = View.Text[..View.MaxLength];

		OnMaxLengthReached(new MaxLengthReachedEventArgs(newTextValue));

		if (Command?.CanExecute(newTextValue) ?? false)
		{
			Command.Execute(newTextValue);
		}
	}

	void OnMaxLengthReached(MaxLengthReachedEventArgs maxLengthReachedEventArgs) =>
		maxLengthReachedEventManager.HandleEvent(this, maxLengthReachedEventArgs, nameof(MaxLengthReached));
}