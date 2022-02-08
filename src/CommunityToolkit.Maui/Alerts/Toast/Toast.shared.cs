using System.ComponentModel;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Alerts;

/// <inheritdoc/>
public partial class Toast : IToast
{
	/// <inheritdoc/>
	public string Text { get; set; } = string.Empty;

	/// <inheritdoc/>
	public ToastDuration Duration { get; set; } = ToastDuration.Short;

	/// <inheritdoc/>
	public double TextSize { get; set; } = Defaults.FontSize;

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

		return new Toast()
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

	private partial void ShowNative(CancellationToken token);

	private partial void DismissNative(CancellationToken token);
}