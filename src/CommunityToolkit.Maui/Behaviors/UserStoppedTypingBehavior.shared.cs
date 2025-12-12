using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="UserStoppedTypingBehavior"/> is a behavior that allows the user to trigger an action when a user has stopped data input any <see cref="InputView"/> derivate like <see cref="Entry"/> or <see cref="SearchBar"/>. Examples of its usage include triggering a search when a user has stopped entering their search query.
/// </summary>
public partial class UserStoppedTypingBehavior : BaseBehavior<InputView>, IDisposable
{
	CancellationTokenSource? tokenSource;
	bool isDisposed;

	/// <inheritdoc />
	~UserStoppedTypingBehavior() => Dispose(false);

	/// <summary>
	/// Command that is triggered when the <see cref="StoppedTypingTimeThreshold" /> is reached. When <see cref="MinimumLengthThreshold"/> is set, it's only triggered when both conditions are met. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial ICommand? Command { get; set; }

	/// <summary>
	/// An optional parameter to forward to the <see cref="Command"/>. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial object? CommandParameter { get; set; }

	/// <summary>
	/// The time of inactivity in milliseconds after which <see cref="Command"/> will be executed. If <see cref="MinimumLengthThreshold"/> is also set, the condition there also needs to be met. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial int StoppedTypingTimeThreshold { get; set; } = UserStoppedTypingBehaviorDefaults.StoppedTypingTimeThreshold;

	/// <summary>
	/// The minimum length of the input value required before <see cref="Command"/> will be executed but only after <see cref="StoppedTypingTimeThreshold"/> has passed. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial int MinimumLengthThreshold { get; set; } = UserStoppedTypingBehaviorDefaults.MinimumLengthThreshold;

	/// <summary>
	/// Indicates whether the keyboard should be dismissed automatically after the user stopped typing. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial bool ShouldDismissKeyboardAutomatically { get; set; } = UserStoppedTypingBehaviorDefaults.ShouldDismissKeyboardAutomatically;

	/// <inheritdoc />
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc/>
	protected override async void OnViewPropertyChanged(InputView sender, PropertyChangedEventArgs e)
	{
		base.OnViewPropertyChanged(sender, e);

		if (e.PropertyName == InputView.TextProperty.PropertyName)
		{
			await OnTextPropertyChanged(sender, sender.Text);
		}
	}

	/// <inheritdoc />
	protected virtual void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			if (disposing)
			{
				tokenSource?.Dispose();
			}

			isDisposed = true;
		}
	}

	async Task OnTextPropertyChanged(InputView view, string? text)
	{
		if (tokenSource is not null)
		{
			await tokenSource.CancelAsync();
			tokenSource.Dispose();
		}

		tokenSource = new CancellationTokenSource();

		if (text is null || text.Length < MinimumLengthThreshold)
		{
			return;
		}

		var task = Task.Delay(StoppedTypingTimeThreshold, tokenSource.Token);
		await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);

		if (task.Status is TaskStatus.Canceled)
		{
			Trace.WriteLine($"{nameof(UserStoppedTypingBehavior)}.{nameof(OnTextPropertyChanged)} cancelled");
			return;
		}

		if (ShouldDismissKeyboardAutomatically)
		{
			Dispatcher.DispatchIfRequired(view.Unfocus);
		}

		if (Command?.CanExecute(CommandParameter ?? text) is true)
		{
			await Dispatcher.DispatchIfRequiredAsync(() => Command.Execute(CommandParameter ?? text));
		}
	}
}