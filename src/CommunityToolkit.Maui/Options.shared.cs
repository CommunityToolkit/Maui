using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
#endif

namespace CommunityToolkit.Maui;

/// <summary>
/// .NET MAUI Community Toolkit Options.
/// </summary>
public class Options : Core.Options
{
	const string windowsAppRuntimeInsightsResourceDll = "Microsoft.WindowsAppRuntime.Insights.Resource.dll";
	const int moduleNotFoundHResult = unchecked((int)0x8007007E);
#if WINDOWS
	static bool isSnackbarNotificationManagerRegistered;
#endif
	readonly MauiAppBuilder? builder;

	internal Options(in MauiAppBuilder builder) : this()
	{
		this.builder = builder;
	}

	internal Options()
	{

	}

	internal static bool ShouldSuppressExceptionsInAnimations { get; private set; }
	internal static bool ShouldSuppressExceptionsInConverters { get; private set; }
	internal static bool ShouldSuppressExceptionsInBehaviors { get; private set; }
	internal static bool ShouldEnableSnackbarOnWindows { get; private set; }
	internal static DefaultPopupSettings DefaultPopupSettings { get; private set; } = new();
	internal static DefaultPopupOptionsSettings DefaultPopupOptionsSettings { get; private set; } = new();

	/// <summary>
	/// Will return the <see cref="ICommunityToolkitValueConverter.DefaultConvertReturnValue"/> default value instead of throwing an exception when using <see cref="BaseConverter{TFrom,TTo}"/>.
	/// </summary>
	/// <remarks>
	/// Default value is false.
	/// </remarks>
	public void SetShouldSuppressExceptionsInConverters(bool value) => ShouldSuppressExceptionsInConverters = value;

	/// <summary>
	/// Catches exceptions thrown when using <see cref="AnimationBehavior"/> and reports it to <see cref="System.Diagnostics.Trace"/>.
	/// </summary>
	/// <remarks>
	/// Default value is false.
	/// </remarks>
	public void SetShouldSuppressExceptionsInAnimations(bool value) => ShouldSuppressExceptionsInAnimations = value;

	/// <summary>
	/// Catches exceptions thrown when using <see cref="AnimationBehavior"/> and reports it to <see cref="System.Diagnostics.Trace"/>.
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
						if (Application.Current is null)
						{
							throw new InvalidOperationException($"{nameof(Application)}.{nameof(Application.Current)} cannot be null when Windows are launched");
						}

						else if (Application.Current.Windows.Count is 1)
						{
							var notificationManager = Microsoft.Windows.AppNotifications.AppNotificationManager.Default;

							try
							{
								notificationManager.Register();
								isSnackbarNotificationManagerRegistered = true;
								notificationManager.NotificationInvoked += OnSnackbarNotificationInvoked;
							}
							// Windows App SDK can omit the Insights resource DLL from self-contained unpackaged apps.
							// Registration then fails, but notifications can still be shown without action callbacks. See https://github.com/microsoft/WindowsAppSDK/issues/6071.
							catch (System.Runtime.InteropServices.COMException exception) when (IsWindowsAppRuntimeModuleUnavailable(exception))
							{
								System.Diagnostics.Trace.WriteLine(
									$"{nameof(Alerts.Snackbar)} action callbacks could not be registered because a Windows App Runtime module is unavailable. Snackbar notifications remain enabled. {exception}");
							}
						}
					})
					.OnClosed((_, _) =>
					{
						if (Application.Current is null)
						{
							throw new InvalidOperationException($"{nameof(Application)}.{nameof(Application.Current)} cannot be null when Windows are closed");
						}
						else if (Application.Current.Windows.Count is 0 && isSnackbarNotificationManagerRegistered)
						{
							Microsoft.Windows.AppNotifications.AppNotificationManager.Default.NotificationInvoked -= OnSnackbarNotificationInvoked;
							Microsoft.Windows.AppNotifications.AppNotificationManager.Default.Unregister();
							isSnackbarNotificationManagerRegistered = false;
						}
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

	/// <summary>
	/// Sets the default settings for <see cref="Popup"/>
	/// </summary>
	/// <param name="globalPopupSettings"></param>
	/// <remarks>The settings passed in here will be set on initialization of every new Popup</remarks>
	public void SetPopupDefaults(DefaultPopupSettings globalPopupSettings)
	{
		DefaultPopupSettings = globalPopupSettings;
	}

	/// <summary>
	/// Sets the default settings for <see cref="PopupOptions"/>
	/// </summary>
	/// <param name="globalPopupOptionsSettings"></param>
	/// <remarks>The settings passed in here will be used when <see cref="PopupExtensions.ShowPopup(Microsoft.Maui.Controls.Page,Microsoft.Maui.Controls.View,CommunityToolkit.Maui.IPopupOptions?)"/> is called the <see cref="CommunityToolkit.Maui.IPopupOptions"/> parameter is null</remarks>
	public void SetPopupOptionsDefaults(DefaultPopupOptionsSettings globalPopupOptionsSettings)
	{
		DefaultPopupOptionsSettings = globalPopupOptionsSettings;
	}

	internal static bool IsWindowsAppRuntimeModuleUnavailable(System.Runtime.InteropServices.COMException exception)
		=> exception.HResult is moduleNotFoundHResult
			&& exception.Message.Contains(windowsAppRuntimeInsightsResourceDll, StringComparison.OrdinalIgnoreCase);
}