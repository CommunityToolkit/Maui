using System.ComponentModel;
using CommunityToolkit.Maui.Core;
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
	bool isDisposed;

	string text = string.Empty;
	ToastDuration duration = ToastDuration.Short;
	double textSize = AlertDefaults.FontSize;

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
		double textSize = AlertDefaults.FontSize)
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
	public virtual Task Show(CancellationToken token = default)
	{
		ShowPlatform(token);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	public virtual Task Dismiss(CancellationToken token = default)
	{
		DismissPlatform(token);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Dispose Toast
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Dispose Toast
	/// </summary>
	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (isDisposing)
		{
#if ANDROID
			PlatformToast?.Dispose();
#endif
		}

		isDisposed = true;
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

#if ANDROID || IOS || MACCATALYST || WINDOWS
	static PlatformToast? PlatformToast { get; set; }
#endif


	private partial void ShowPlatform(CancellationToken token);

	private static partial void DismissPlatform(CancellationToken token);

#if !(IOS || ANDROID || MACCATALYST || WINDOWS)
	/// <summary>
	/// Show Toast
	/// </summary>
#pragma warning disable CA1822 // Cannot mark as static as Toast.android.cs requires instance
	private partial void ShowPlatform(CancellationToken token)
#pragma warning restore CA1822 // Cannot mark as static as Toast.android.cs requires instance
    {
        token.ThrowIfCancellationRequested();
	}

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	private static partial void DismissPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
	}
#endif
}