using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Dispatching;
#if ANDROID
using PlatformToast = Android.Widget.Toast;
#elif IOS || MACCATALYST
using PlatformToast = CommunityToolkit.Maui.Core.Views.PlatformToast;
#elif WINDOWS
using PlatformToast = Windows.UI.Notifications.ToastNotification;
#endif

namespace CommunityToolkit.Maui.Alerts;

/// <inheritdoc/>
public partial class Toast : IToast
{
	string text = string.Empty;
	ToastDuration duration = ToastDuration.Short;
	double textSize = Defaults.FontSize;

	/// <inheritdoc/>
	public string Text
	{
		get => text;
		init => text = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc/>
	public ToastDuration Duration
	{
		get => duration;
		init
		{
			if (!Enum.IsDefined(typeof(ToastDuration), value))
			{
				throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ToastDuration));
			}

			duration = value;
		}
	}

	/// <inheritdoc/>
	public double TextSize
	{
		get => textSize;
		init
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "Toast font size must be positive");
			}

			textSize = value;
		}
	}

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
	public virtual Task Show(CancellationToken token = default) => Dispatcher.GetForCurrentThread().DispatchIfRequiredAsync(() => ShowPlatform(token));

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	public virtual Task Dismiss(CancellationToken token = default) => Dispatcher.GetForCurrentThread().DispatchIfRequiredAsync(() => DismissPlatform(token));

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
		await Dispatcher.GetForCurrentThread().DispatchIfRequiredAsync(() => PlatformToast?.Dispose());
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
	static PlatformToast? PlatformToast { get; set; }
#endif


	private partial void ShowPlatform(CancellationToken token);

	private partial void DismissPlatform(CancellationToken token);

#if !(IOS || ANDROID || MACCATALYST || WINDOWS)
	/// <summary>
	/// Show Toast
	/// </summary>
	private partial void ShowPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
	}

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	private partial void DismissPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
	}
#endif
}