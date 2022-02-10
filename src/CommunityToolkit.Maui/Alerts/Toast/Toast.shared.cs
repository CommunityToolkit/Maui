using System.ComponentModel;
using CommunityToolkit.Maui.Core;
#if ANDROID
using NativeToast = Android.Widget.Toast;
#elif IOS || MACCATALYST
using NativeToast = CommunityToolkit.Maui.Core.Views.ToastView;
#elif WINDOWS
using NativeToast = Windows.UI.Notifications.ToastNotification;
#endif

namespace CommunityToolkit.Maui.Alerts;

/// <inheritdoc/>
public partial class Toast : IToast
{
	/// <inheritdoc/>
	public string Text { get; init; } = string.Empty;

	/// <inheritdoc/>
	public ToastDuration Duration { get; init; } = ToastDuration.Short;

	/// <inheritdoc/>
	public double TextSize { get; init; } = Defaults.FontSize;

	/// <summary>
	/// Create new Toast
	/// </summary>
	/// <param name="message">Toast message</param>
	/// <param name="duration">Toast duration</param>
	/// <param name="textSize">Toast font size</param>
	/// <returns>New instance of Toast</returns>
	public static IToast Make(
		string message,
		ToastDuration duration = ToastDuration.Short,
		double textSize = Defaults.FontSize)
	{
		ArgumentNullException.ThrowIfNull(message);

		if (!Enum.IsDefined(typeof(ToastDuration), duration))
		{
			throw new InvalidEnumArgumentException(nameof(duration), (int)duration, typeof(ToastDuration));
		}

		if (textSize <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(textSize), "Toast font size must be positive");
		}

		return new Toast
		{
			Text = message,
			Duration = duration,
			TextSize = textSize
		};
	}

	/// <summary>
	/// Show Toast
	/// </summary>
	public virtual Task Show(CancellationToken token = default) => Device.InvokeOnMainThreadAsync(() => ShowNative(token));

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	public virtual Task Dismiss(CancellationToken token = default) => Device.InvokeOnMainThreadAsync(() => DismissNative(token));

	/// <summary>
	/// Dispose Toast
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		await DisposeAsyncCore();
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Dispose Toast
	/// </summary>
#if ANDROID
	protected virtual async ValueTask DisposeAsyncCore()
	{
		await Device.InvokeOnMainThreadAsync(() => NativeToast?.Dispose());
	}
#else
	protected virtual ValueTask DisposeAsyncCore()
	{
		return ValueTask.CompletedTask;
    }
#endif

#if IOS || MACCATALYST || WINDOWS
	static TimeSpan GetDuration(ToastDuration duration)
	{
		return duration switch
		{
			ToastDuration.Short => TimeSpan.FromSeconds(2),
			ToastDuration.Long => TimeSpan.FromSeconds(3.5),
			_ => throw new InvalidEnumArgumentException(nameof(Duration), (int)duration, typeof(ToastDuration))
		};
	}
#endif

#if ANDROID || IOS || MACCATALYST || WINDOWS
	static NativeToast? nativeToast;

	static NativeToast? NativeToast
	{
		get
		{
			return MainThread.IsMainThread
				? nativeToast
				: throw new InvalidOperationException($"{nameof(nativeToast)} can only be called from the Main Thread");
		}
		set
		{
			if (!MainThread.IsMainThread)
			{
				throw new InvalidOperationException($"{nameof(nativeToast)} can only be called from the Main Thread");
			}

			nativeToast = value;
		}
	}
#endif


	private partial void ShowNative(CancellationToken token);

	private partial void DismissNative(CancellationToken token);

#if !(IOS || ANDROID || MACCATALYST || WINDOWS)
	/// <summary>
	/// Show Toast
	/// </summary>
	private partial void ShowNative(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
	}

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	private partial void DismissNative(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
	}
#endif
}