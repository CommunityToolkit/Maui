using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CommunityToolkit.Maui.Alerts;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Xunit.Sdk;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;

namespace CommunityToolkit.Maui.DeviceTests;

// An app notification as the Windows App SDK describes it: the texts, the buttons and the duration are read from the payload XML that
// the SDK itself built from the toolkit's AppNotificationBuilder calls.
sealed record WindowsNotification(AppNotification Notification, IReadOnlyList<string> Texts, IReadOnlyList<WindowsNotificationButton> Buttons, string? Duration)
{
	// A Snackbar is the notification with an action button, a Toast has none.
	public bool IsSnackbar => Buttons.Count > 0;

	public string Text => string.Concat(Texts);
}

sealed record WindowsNotificationButton(string Content, string Arguments)
{
	// The Snackbar identifies the action of a button by the snackbarIdentifier argument.
	public string? SnackbarIdentifier => Regex.Match(Arguments, "snackbarIdentifier=([^;&]+)") is { Success: true } match ? match.Groups[1].Value : null;
}

// Reads the app notifications that Snackbar and Toast show (they are listed in the Action Center of the app), and the toolkit's private
// static state that goes with them.
static partial class AlertsWindows
{
	const string probeText = "alerts-capability-probe";

	static bool isProbed;

	// Why the app cannot show notifications in this session (null when it can), and why Windows does not list them back (null when it does;
	// never null while notifications cannot be shown).
	static string? cannotShowReason;
	static string? cannotListReason;

	public static WindowsNotification Parse(AppNotification notification)
	{
		var document = XDocument.Parse(notification.Payload);
		var texts = document.Descendants("text").Select(element => element.Value).ToList();
		var buttons = document.Descendants("action")
			.Select(element => new WindowsNotificationButton((string?)element.Attribute("content") ?? string.Empty, (string?)element.Attribute("arguments") ?? string.Empty))
			.ToList();

		return new WindowsNotification(notification, texts, buttons, (string?)document.Root?.Attribute("duration"));
	}

	// Every notification of the app that is listed in the Action Center. Skips the calling test when Windows cannot list notifications.
	public static async Task<IReadOnlyList<WindowsNotification>> GetDisplayedAsync()
	{
		await SkipWhenNotificationsAreUnavailableAsync();

		return [.. (await AppNotificationManager.Default.GetAllAsync()).Select(Parse)];
	}

	// Asks the platform once per process whether this app can show a notification and whether the notification is listed back. The probe
	// shows a notification and removes every notification of the app, so it has to run before a test shows its own:
	// AlertsTestBase.PreparePlatformAsync calls it.
	public static async Task EnsureProbedAsync()
	{
		if (!isProbed)
		{
			await ProbeAsync();
			isProbed = true;
		}
	}

	// Skips the calling test when this app cannot show notifications in this session (AppNotificationManager is not supported, or showing
	// the probe's notification failed): every test that shows a Snackbar or a Toast would fail for that reason alone.
	// AlertsTestBase.PreparePlatformAsync calls it.
	public static async Task SkipWhenNotificationsCannotBeShownAsync()
	{
		await EnsureProbedAsync();

		Skip.If(cannotShowReason is not null, cannotShowReason ?? string.Empty);
	}

	// Skips the calling test unless a notification that this app shows is listed back by the platform. Windows does that unless
	// notifications are turned off for the app or the user, or the session has no notification platform.
	public static async Task SkipWhenNotificationsAreUnavailableAsync()
	{
		await EnsureProbedAsync();

		Skip.If(cannotListReason is not null, cannotListReason ?? string.Empty);
	}

	// Removes the notifications of earlier tests. Best effort: a session without a notification platform has nothing to remove.
	public static async Task TryRemoveAllAsync()
	{
		try
		{
			await AppNotificationManager.Default.RemoveAllAsync();
		}
		catch (Exception exception) when (IsNotificationPlatformFailure(exception))
		{
			// Notifications are not available in this session.
		}
	}

	// Snackbar.actions: the action of every notification that is shown, by the identifier that is in the button's arguments.
	public static Dictionary<string, Action?> GetRegisteredActions() =>
		(Dictionary<string, Action?>)(typeof(Snackbar).GetField("actions", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null)
			?? throw new InvalidOperationException("Snackbar.actions was not found."));

	public static AppNotification? GetPlatformSnackbar() => (AppNotification?)typeof(Snackbar).GetProperty("PlatformSnackbar", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);

	public static AppNotification? GetPlatformToast() => (AppNotification?)typeof(Toast).GetProperty("PlatformToast", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);

	// What the Windows App SDK throws when the session has no notification platform, or the app cannot use it.
	static bool IsNotificationPlatformFailure(Exception exception) =>
		exception is COMException or InvalidOperationException or NotSupportedException or UnauthorizedAccessException;

	static async Task ProbeAsync()
	{
		AppNotificationManager manager;
		try
		{
			if (!AppNotificationManager.IsSupported())
			{
				cannotShowReason = cannotListReason = "AppNotificationManager.IsSupported() is false for this app, so it cannot show notifications.";
				return;
			}

			manager = AppNotificationManager.Default;
			await manager.RemoveAllAsync();
			manager.Show(new AppNotificationBuilder().AddText(probeText).BuildNotification());
		}
		catch (Exception exception) when (IsNotificationPlatformFailure(exception))
		{
			cannotShowReason = cannotListReason = $"The Windows notification platform cannot show the notifications of this app in this session ({exception.GetType().Name}: {exception.Message}).";
			return;
		}

		try
		{
			if (manager.Setting is not AppNotificationSetting.Enabled)
			{
				cannotListReason = $"App notifications are {manager.Setting} for this app, so Windows does not display or list them.";
			}
			else
			{
				try
				{
					await AssertEventually(async () => (await manager.GetAllAsync()).Any(notification => notification.Payload.Contains(probeText, StringComparison.Ordinal)), timeout: 10000);
				}
				catch (XunitException)
				{
					cannotListReason = "Windows did not list a notification that the app showed, so there is no notification platform in this session.";
				}
			}

			await manager.RemoveAllAsync();
		}
		catch (Exception exception) when (IsNotificationPlatformFailure(exception))
		{
			cannotListReason = $"Windows cannot list the notifications of this app in this session ({exception.GetType().Name}: {exception.Message}).";
		}
	}
}