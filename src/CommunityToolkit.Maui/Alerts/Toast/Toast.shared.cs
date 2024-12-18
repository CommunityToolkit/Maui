using System.ComponentModel;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Alerts;

/// <inheritdoc/>
public partial class Toast : IToast
{
	bool isDisposed;

	/// <inheritdoc/>
	public string Text
	{
		get;
		init => field = value ?? throw new ArgumentNullException(nameof(value));
	} = string.Empty;

	/// <inheritdoc/>
	public ToastDuration Duration
	{
		get;
		init
		{
			if (!Enum.IsDefined(value))
			{
				throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ToastDuration));
			}

			field = value;
		}
	} = ToastDuration.Short;

	/// <inheritdoc/>
	public double TextSize
	{
		get;
		init
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "Toast font size must be positive");
			}

			field = value;
		}
	} = AlertDefaults.FontSize;

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
#if WINDOWS
		return ShowPlatform(token);
#else
		ShowPlatform(token);
		return Task.CompletedTask;
#endif
	}

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	public virtual Task Dismiss(CancellationToken token = default)
	{
#if WINDOWS
		return DismissPlatform(token);
#else
		DismissPlatform(token);
		return Task.CompletedTask;
#endif
	}

	/// <summary>
	/// Dispose Toast
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	static TimeSpan GetDuration(ToastDuration duration) => duration switch
	{
		ToastDuration.Short => TimeSpan.FromSeconds(2),
		ToastDuration.Long => TimeSpan.FromSeconds(3.5),
		_ => throw new InvalidEnumArgumentException(nameof(Duration), (int)duration, typeof(ToastDuration))
	};
}