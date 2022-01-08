using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Alerts;

/// <inheritdoc/>
public partial class Toast : IToast
{
	/// <inheritdoc/>
	public string Text { get; set; } = string.Empty;

	/// <inheritdoc/>
	public ToastDuration Duration { get; set; }

	/// <summary>
	/// Create new Toast
	/// </summary>
	/// <param name="message">Toast message</param>
	/// <param name="duration">Toast duration</param>
	/// <returns>New instance of Toast</returns>
	public static IToast Make(
		string message,
		ToastDuration duration = ToastDuration.Short)
	{
		return new Toast()
		{
			Text = message,
			Duration = duration
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
	
	public virtual partial Task Show(CancellationToken token)
	{
		return Task.CompletedTask;
	}

	public virtual partial Task Dismiss(CancellationToken token)
	{
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
		await Device.InvokeOnMainThreadAsync(() => _nativeToast?.Dispose());
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
			_ => throw new ArgumentOutOfRangeException(nameof(duration), duration, null)
		};
	}
#endif
}