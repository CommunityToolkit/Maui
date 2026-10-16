using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using ToolkitDispatcherExtensions = CommunityToolkit.Maui.Core.Extensions.DispatcherExtensions;

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
	/// <remarks>
	/// The value must be 0 or greater. Assigning a negative value, also through a binding, does not throw;
	/// the assignment is ignored and the previous value is kept (.NET MAUI logs a warning when a logger is available).
	/// </remarks>
	[BindableProperty(ValidateValueMethodName = nameof(ValidateStoppedTypingTimeThreshold))]
	public partial int StoppedTypingTimeThreshold { get; set; } = UserStoppedTypingBehaviorDefaults.StoppedTypingTimeThreshold;

	/// <summary>
	/// The minimum length of the input value required before <see cref="Command"/> will be executed but only after <see cref="StoppedTypingTimeThreshold"/> has passed. This is a bindable property.
	/// </summary>
	[BindableProperty]
	public partial int MinimumLengthThreshold { get; set; } = UserStoppedTypingBehaviorDefaults.MinimumLengthThreshold;

	/// <summary>
	/// Indicates whether the keyboard should be dismissed automatically after the user stopped typing. This is a bindable property.
	/// </summary>
	/// <remarks>
	/// Dismissing the keyboard also removes the focus from the <see cref="InputView"/>.
	/// </remarks>
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

	// Task.Delay throws ArgumentOutOfRangeException for a delay below -1 and never completes for -1 (Timeout.Infinite)
	static bool ValidateStoppedTypingTimeThreshold(BindableObject bindable, object value) => value is int threshold && threshold >= 0;

	async Task OnTextPropertyChanged(InputView view, string? text)
	{
		// Replace the token source before anything is awaited: when an await came between cancelling the previous source and replacing it,
		// a Text change handled meanwhile could dispose a newer source without cancelling it, and outdated text then executed the Command
		var newTokenSource = new CancellationTokenSource();
		var token = newTokenSource.Token;
		var previousTokenSource = Interlocked.Exchange(ref tokenSource, newTokenSource);
		previousTokenSource?.Cancel();
		previousTokenSource?.Dispose();

		if (text is null || text.Length < MinimumLengthThreshold)
		{
			return;
		}

		var task = Task.Delay(StoppedTypingTimeThreshold, token);
		await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);

		// A newer Text change also cancels the token after the delay completed, while this continuation waited for the UI thread
		if (task.Status is TaskStatus.Canceled || token.IsCancellationRequested)
		{
			Trace.WriteLine($"{nameof(UserStoppedTypingBehavior)}.{nameof(OnTextPropertyChanged)} cancelled");
			return;
		}

		if (ShouldDismissKeyboardAutomatically)
		{
			await ToolkitDispatcherExtensions.DispatchIfRequiredAsync(Dispatcher, view.DismissKeyboard);
		}

		if (Command?.CanExecute(CommandParameter ?? text) is true)
		{
			await ToolkitDispatcherExtensions.DispatchIfRequiredAsync(Dispatcher, () => Command.Execute(CommandParameter ?? text));
		}
	}
}