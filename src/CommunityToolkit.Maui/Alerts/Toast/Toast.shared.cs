using CommunityToolkit.Maui.Core;
using System.ComponentModel;

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
	public virtual partial Task Show(CancellationToken token = default);

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	public virtual partial Task Dismiss(CancellationToken token = default);

#if !(IOS || ANDROID || MACCATALYST || WINDOWS)
	/// <summary>
	/// Show Toast
	/// </summary>
	public virtual partial Task Show(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	public virtual partial Task Dismiss(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		return Task.CompletedTask;
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
	protected virtual async ValueTask DisposeAsyncCore()
	{
#if ANDROID
		await Device.InvokeOnMainThreadAsync(() => nativeToast?.Dispose());
#else
		await Task.CompletedTask;
#endif
	}

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
}