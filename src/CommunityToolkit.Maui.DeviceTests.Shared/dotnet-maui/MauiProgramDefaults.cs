// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/Core/tests/DeviceTests.Shared/MauiProgramDefaults.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespace changed from Microsoft.Maui(.DeviceTests) to CommunityToolkit.Maui.DeviceTests.
// Toolkit change: UI.Xaml.Window is fully qualified and Microsoft.Maui is imported (the enclosing Microsoft(.Maui) namespaces no longer resolve them).
// Toolkit change: CreateMauiApp takes an optional configureApp callback so each app registers its toolkit package on the runner's MauiApp.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using CommunityToolkit.Maui.DeviceTests.Runners;

namespace CommunityToolkit.Maui.DeviceTests
{
	public static class MauiProgramDefaults
	{
#if ANDROID
		public static global::Android.Content.Context DefaultContext { get; private set; }
#elif WINDOWS
		public static Microsoft.UI.Xaml.Window DefaultWindow { get; private set; }
#endif

		public static IApplication DefaultTestApp { get; private set; }

		public static MauiApp CreateMauiApp(List<Assembly> testAssemblies)
		{
			return CreateMauiApp((_) => new TestOptions
			{
				Assemblies = testAssemblies
			});
		}

		public static MauiApp CreateMauiApp(Func<IServiceProvider, TestOptions> options, Action<MauiAppBuilder> configureApp = null)
		{
			var appBuilder = MauiApp.CreateBuilder();

#if DEBUG
			appBuilder.Services.AddHybridWebViewDeveloperTools();
#endif

			appBuilder
				.ConfigureLifecycleEvents(life =>
				{
#if ANDROID
					life.AddAndroid(android =>
					{
						android.OnCreate((a, b) =>
						{
							DefaultContext = a;
						});
					});
#elif WINDOWS
					life.AddWindows(windows =>
					{
						windows.OnWindowCreated((w) => DefaultWindow = w);
					});
#endif
				})
				.ConfigureTests(options);

#if WINDOWS
			if (options(null).Assemblies.Any(a => a.FullName.Contains("Controls.DeviceTests",
				StringComparison.OrdinalIgnoreCase)))
			{
				appBuilder.UseControlsHeadlessRunner(new HeadlessRunnerOptions
				{
					RequiresUIContext = true,
				});
			}
			else
			{
				appBuilder.UseHeadlessRunner(new HeadlessRunnerOptions
				{
					RequiresUIContext = true,
				});
			}
#else
			appBuilder.UseHeadlessRunner(new HeadlessRunnerOptions
			{
				RequiresUIContext = true,
			});
#endif

#if IOS || MACCATALYST

			appBuilder.ConfigureMauiHandlers(handlers =>
			{
				handlers.AddHandler<Microsoft.Maui.Controls.CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
				handlers.AddHandler<Microsoft.Maui.Controls.CarouselView, Microsoft.Maui.Controls.Handlers.Items2.CarouselViewHandler2>();
			});

#endif
			appBuilder.UseVisualRunner();

			configureApp?.Invoke(appBuilder);

			appBuilder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
			{
				ValidateOnBuild = true,
				ValidateScopes = true,
			}));

			var mauiApp = appBuilder.Build();

			DefaultTestApp = mauiApp.Services.GetRequiredService<IApplication>();

			return mauiApp;
		}
	}
}