using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Dispatching;

#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif ANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#elif TIZEN
using PlatformView = Tizen.NUI.BaseComponents.View;
#elif (NETSTANDARD || !PLATFORM) || (NET6_0_OR_GREATER && !IOS && !ANDROID && !TIZEN)
using PlatformView = System.Object;
#endif

namespace CommunityToolkit.Maui.Core.Platform;

/// <summary>
/// Extension methods for interacting with a platforms Soft Keyboard Pane
/// </summary>
public static partial class KeyboardExtensions
{
	/// <summary>
	/// If a soft input device is currently showing, this will attempt to hide it.
	/// </summary>
	/// <param name="targetView"></param>
	/// <param name="token">Cancellation token</param>
	/// <returns>
	/// Returns <c>true</c> if the platform was able to hide the soft input device.</returns>
	/// <remarks>
	/// On iOS and Windows the soft keyboard is hidden on the UI thread: called from another thread, the returned task completes once the UI thread has hidden it,
	/// and fails with <see cref="InvalidOperationException"/> when the dispatcher cannot queue the call.
	/// </remarks>
	public static ValueTask<bool> HideKeyboardAsync(this ITextInput targetView, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		if (!targetView.TryGetPlatformView(out var platformView, out var handler, out _))
		{
			return ValueTask.FromResult(false);
		}

		return CallThePlatformAsync(handler, () => HideKeyboard(platformView), token);
	}

	/// <summary>
	/// If a soft input device is currently hiding, this will attempt to show it.
	/// </summary>
	/// <param name="targetView"></param>
	/// <param name="token">Cancellation token</param>
	/// <returns>
	/// Returns <c>true</c> if the platform was able to show the soft input device.</returns>
	/// <remarks>
	/// A view that is not focused is focused first, and its soft keyboard is then shown on the UI thread. On iOS and Windows the soft keyboard of a focused view
	/// is shown on the UI thread too. When the UI thread shows the soft keyboard, the returned task completes once it has shown it, and fails with
	/// <see cref="InvalidOperationException"/> when the dispatcher cannot queue the call.
	/// </remarks>
	public static async ValueTask<bool> ShowKeyboardAsync(this ITextInput targetView, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();

		if (!targetView.TryGetPlatformView(out var platformView, out var handler, out var view))
		{
			return false;
		}

		if (!view.IsFocused)
		{
			var focusRequest = new FocusRequest();
			focusRequest.SetResult(false);
			await CallOnUIThreadAsync(handler, () => handler.Invoke(nameof(IView.Focus), focusRequest), token);

			// Queued after the work that the Focus command queued on the UI thread (Android posts RequestFocus), also for a caller on the UI thread
			var result = await QueueOnUIThreadAsync(handler.GetRequiredService<IDispatcher>(), () => platformView.ShowKeyboard(), token).ConfigureAwait(false);
			return result;
		}
		else
		{
			var result = await CallThePlatformAsync(handler, () => platformView.ShowKeyboard(), token).ConfigureAwait(false);
			return result;
		}
	}

	/// <summary>
	/// Checks to see if the platform is currently showing the soft input pane
	/// </summary>
	/// <param name="targetView"></param>
	/// <returns>
	/// Returns <c>true</c> if the soft input device is currently showing.</returns>
	/// <remarks>
	/// On iOS and Windows the soft keyboard state is read on the UI thread: called from another thread, this method blocks until the UI thread has answered.
	/// </remarks>
	public static bool IsSoftKeyboardShowing(this ITextInput targetView)
	{
		if (!targetView.TryGetPlatformView(out var platformView, out var handler, out _))
		{
			throw new SoftKeyboardException($"Unable to retrieve {typeof(PlatformView)} to determine soft keyboard status");
		}

		// This method is synchronous: where the platform view must be used on the UI thread, a caller on another thread waits for its answer
		if (IsDispatchRequiredByThePlatform(handler, out var dispatcher))
		{
			var isShowing = false;
			dispatcher.DispatchIfRequiredAsync(() => isShowing = platformView.IsSoftKeyboardShowing()).GetAwaiter().GetResult();
			return isShowing;
		}

		return platformView.IsSoftKeyboardShowing();
	}

	static bool TryGetPlatformView(this ITextInput textInput,
		[NotNullWhen(true)] out PlatformView? platformView,
		[NotNullWhen(true)] out IViewHandler? handler,
		[NotNullWhen(true)] out IView? view)
	{
		if (textInput is not IView { Handler: IViewHandler viewHandler } iView)
		{
			platformView = null;
			handler = null;
			view = null;

			return false;
		}

		if (iView.Handler?.PlatformView is not PlatformView platform)
		{
			platformView = null;
			handler = null;
			view = null;

			return false;
		}

		handler = viewHandler;
		platformView = platform;
		view = iView;

		return true;
	}

	// Platform views must only be used on their UI thread: returns the dispatcher of that thread when the caller is on another thread.
	// A handler without a MauiContext has no dispatcher: the platform view is then used directly, as on the UI thread.
	static bool IsDispatchRequired(IViewHandler handler, [NotNullWhen(true)] out IDispatcher? dispatcher)
	{
		dispatcher = handler.MauiContext?.Services.GetService<IDispatcher>();
		if (dispatcher is { IsDispatchRequired: true })
		{
			return true;
		}

		dispatcher = null;
		return false;
	}

	// Whether the soft keyboard calls of the platform view (hide, show, is showing) must switch to the UI thread. UIKit (iOS) and WinUI
	// (Windows: the XamlRoot of the view) must only be used on the UI thread. Android's input method manager and window insets can be used
	// on any thread, and Mac Catalyst does not support the soft keyboard: there the platform view is called on the calling thread.
	static bool IsDispatchRequiredByThePlatform(IViewHandler handler, [NotNullWhen(true)] out IDispatcher? dispatcher)
	{
#if IOS || WINDOWS
		return IsDispatchRequired(handler, out dispatcher);
#else
		dispatcher = null;
		return false;
#endif
	}

	// Calls the platform on the UI thread: directly when the caller is on it, otherwise through the dispatcher of the handler, which throws
	// InvalidOperationException when it cannot queue the call, and skips the call when the token is cancelled before the UI thread runs it
	static ValueTask CallOnUIThreadAsync(IViewHandler handler, Action platformCall, CancellationToken token)
	{
		if (IsDispatchRequired(handler, out var dispatcher))
		{
			return new ValueTask(dispatcher.DispatchIfRequiredAsync(platformCall, token));
		}

		platformCall();
		return ValueTask.CompletedTask;
	}

	// Calls the platform view on the calling thread, or, where the platform requires it, on the UI thread as CallOnUIThreadAsync does
	static ValueTask<bool> CallThePlatformAsync(IViewHandler handler, Func<bool> platformCall, CancellationToken token)
	{
		if (IsDispatchRequiredByThePlatform(handler, out var dispatcher))
		{
			return CallThroughTheDispatcherAsync(dispatcher, platformCall, token);
		}

		return ValueTask.FromResult(platformCall());

		static async ValueTask<bool> CallThroughTheDispatcherAsync(IDispatcher dispatcher, Func<bool> platformCall, CancellationToken token)
		{
			var result = false;
			await dispatcher.DispatchIfRequiredAsync(() => result = platformCall(), token).ConfigureAwait(false);
			return result;
		}
	}

	// Queues the platform call on the UI thread, also when the caller is on it: throws InvalidOperationException when the dispatcher cannot
	// queue the call, skips the call when the token is cancelled before the UI thread runs it, and does not run the caller's continuation on
	// the UI thread.
	static async Task<bool> QueueOnUIThreadAsync(IDispatcher dispatcher, Func<bool> platformCall, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		await using var cancellationTokenRegistration = token.Register(static (state, cancellationToken) =>
			((TaskCompletionSource<bool>?)state)?.TrySetCanceled(cancellationToken), taskCompletionSource);

		if (!dispatcher.Dispatch(() =>
			{
				if (token.IsCancellationRequested)
				{
					taskCompletionSource.TrySetCanceled(token);
					return;
				}

				try
				{
					taskCompletionSource.TrySetResult(platformCall());
				}
				catch (Exception exception)
				{
					taskCompletionSource.TrySetException(exception);
				}
			}))
		{
			throw new InvalidOperationException("The dispatcher was unable to queue the requested action.");
		}

		return await taskCompletionSource.Task.ConfigureAwait(false);
	}

	/// <summary>
	/// An <see cref="Exception"/> thrown when unable to retrieve the platform view to determine soft keyboard status.
	/// </summary>
	public sealed class SoftKeyboardException(string message) : Exception(message)
	{
	}
}