using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Converters;
using Microsoft.Maui.LifecycleEvents;

namespace CommunityToolkit.Maui;

/// <summary>
/// .NET MAUI Community Toolkit Options.
/// </summary>
public class Options() : Core.Options
{
	readonly MauiAppBuilder? builder;

	internal Options(in MauiAppBuilder builder) : this()
	{
		this.builder = builder;
	}

	internal static bool ShouldSuppressExceptionsInAnimations { get; private set; }
	internal static bool ShouldSuppressExceptionsInConverters { get; private set; }
	internal static bool ShouldSuppressExceptionsInBehaviors { get; private set; }
	internal static bool ShouldEnableSnackbarOnWindows { get; private set; }

	/// <summary>
	/// Allows to return default value instead of throwing an exception when using <see cref="BaseConverter{TFrom,TTo}"/>.
	/// </summary>
	/// <remarks>
	/// Default value is false.
	/// </remarks>
	public void SetShouldSuppressExceptionsInConverters(bool value) => ShouldSuppressExceptionsInConverters = value;

	/// <summary>
	/// Allows to return default value instead of throwing an exception when using <see cref="AnimationBehavior"/>.
	/// </summary>
	/// <remarks>
	/// Default value is false.
	/// </remarks>
	public void SetShouldSuppressExceptionsInAnimations(bool value) => ShouldSuppressExceptionsInAnimations = value;

	/// <summary>
	/// Allows to return default value instead of throwing an exception when using <see cref="BaseBehavior{TView}"/>.
	/// </summary>
	/// <remarks>
	/// Default value is false.
	/// </remarks>
	public void SetShouldSuppressExceptionsInBehaviors(bool value) => ShouldSuppressExceptionsInBehaviors = value;

	/// <summary>
	/// Enables <see cref="Alerts.Snackbar"/> for Windows
	/// </summary>
	/// <remarks>
	/// Additional setup is required in the Package.appxmanifest file to enable <see cref="Alerts.Snackbar"/> on Windows. See the <a href="https://learn.microsoft.com/dotnet/communitytoolkit/maui/alerts/snackbar">Snackbar Platform Specific Initialization Documentation</a> for more information. Default value is false.
	/// </remarks>
	public void SetShouldEnableSnackbarOnWindows(bool value)
	{
#if WINDOWS
		if (value is true && builder is null)
		{
			throw new InvalidOperationException($"{nameof(SetShouldEnableSnackbarOnWindows)} must be called using the {nameof(AppBuilderExtensions.UseMauiCommunityToolkit)} extension method. See the Platform Specific Initialization section of the {nameof(Alerts.Snackbar)} documentaion for more inforamtion: https://learn.microsoft.com/dotnet/communitytoolkit/maui/alerts/snackbar)")
			{
 				HelpLink = "https://learn.microsoft.com/dotnet/communitytoolkit/maui/alerts/snackbar"
 			};
		}
		else if (value is true && builder is not null)
		{
			builder.ConfigureLifecycleEvents(events =>
			{
				events.AddWindows(windows => windows
					.OnLaunched((_, _) =>
					{
						Microsoft.Windows.AppNotifications.AppNotificationManager.Default.NotificationInvoked += OnSnackbarNotificationInvoked;
						Microsoft.Windows.AppNotifications.AppNotificationManager.Default.Register();
					})
					.OnClosed((_, _) =>
					{
						Microsoft.Windows.AppNotifications.AppNotificationManager.Default.NotificationInvoked -= OnSnackbarNotificationInvoked;
						Microsoft.Windows.AppNotifications.AppNotificationManager.Default.Unregister();
					}));
			});

			static void OnSnackbarNotificationInvoked(Microsoft.Windows.AppNotifications.AppNotificationManager sender,
														Microsoft.Windows.AppNotifications.AppNotificationActivatedEventArgs args)
			{
				Alerts.Snackbar.HandleSnackbarAction(args);
			}
		}
#endif

		ShouldEnableSnackbarOnWindows = value;
	}
}