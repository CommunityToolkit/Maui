using System.Diagnostics.CodeAnalysis;
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
using IPlatformViewHandler = Microsoft.Maui.IViewHandler;
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
	public static ValueTask<bool> HideKeyboardAsync(this ITextInput targetView, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		if (!targetView.TryGetPlatformView(out var platformView, out _, out _))
		{
			return ValueTask.FromResult(false);
		}

		return ValueTask.FromResult(HideKeyboard(platformView));
	}

	/// <summary>
	/// If a soft input device is currently hiding, this will attempt to show it.
	/// </summary>
	/// <param name="targetView"></param>
	/// <param name="token">Cancellation token</param>
	/// <returns>
	/// Returns <c>true</c> if the platform was able to show the soft input device.</returns>
	public static Task<bool> ShowKeyboardAsync(this ITextInput targetView, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();

		if (!targetView.TryGetPlatformView(out var platformView, out var handler, out var view))
		{
			return Task.FromResult(false);
		}

		if (!view.IsFocused)
		{
			var showKeyboardTCS = new TaskCompletionSource<bool>();

			var focusRequest = new FocusRequest();
			focusRequest.SetResult(false);
			handler.Invoke(nameof(IView.Focus), focusRequest);

			handler.GetRequiredService<IDispatcher>().Dispatch(() =>
			{
				try
				{
					var result = platformView.ShowKeyboard();
					showKeyboardTCS.SetResult(result);
				}
				catch (Exception e)
				{
					showKeyboardTCS.SetException(e);
				}
			});

			return showKeyboardTCS.Task.WaitAsync(token);
		}
		else
		{
			var result = platformView.ShowKeyboard();
			return Task.FromResult(result).WaitAsync(token);
		}
	}

	/// <summary>
	/// Checks to see if the platform is currently showing the soft input pane
	/// </summary>
	/// <param name="targetView"></param>
	/// <returns>
	/// Returns <c>true</c> if the soft input device is currently showing.</returns>
	public static bool IsSoftKeyboardShowing(this ITextInput targetView)
	{
		if (!targetView.TryGetPlatformView(out var platformView, out _, out _))
		{
			throw new SoftKeyboardException($"Unable to retrive {typeof(PlatformView)} to determine soft keyboard status");
		}

		return platformView.IsSoftKeyboardShowing();
	}

	static bool TryGetPlatformView(this ITextInput textInput,
									[NotNullWhen(true)] out PlatformView? platformView,
									[NotNullWhen(true)] out IPlatformViewHandler? handler,
									[NotNullWhen(true)] out IView? view)
	{
		if (textInput is not IView iView ||
			iView.Handler is not IPlatformViewHandler platformViewHandler)
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

		handler = platformViewHandler;
		platformView = platform;
		view = iView;

		return true;
	}

	sealed class SoftKeyboardException(string message) : Exception(message)
	{
	}
}