// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/AppHostBuilderExtensions.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
// Toolkit change: ConfigureTests registers TestApplicationDispatcher (public-API replacement for MAUI's internal ApplicationDispatcher).
#nullable enable
using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner;
using CommunityToolkit.Maui.DeviceTests.Runners.VisualRunner;

namespace CommunityToolkit.Maui.DeviceTests.Runners
{
	public static class AppHostBuilderExtensions
	{
		public static MauiAppBuilder ConfigureTests(this MauiAppBuilder appHostBuilder, TestOptions options)
		{
			appHostBuilder.Services.AddSingleton(options);
			appHostBuilder.AddTestApplicationDispatcher();

			appHostBuilder.Logging.AddConsole();
			// appHostBuilder.Logging.SetMinimumLevel(LogLevel.Debug);
			return appHostBuilder;
		}

		public static MauiAppBuilder ConfigureTests(this MauiAppBuilder appHostBuilder, Func<IServiceProvider, TestOptions> options)
		{
			appHostBuilder.Services.AddSingleton(options);
			appHostBuilder.AddTestApplicationDispatcher();

			appHostBuilder.Logging.AddConsole();
			// appHostBuilder.Logging.SetMinimumLevel(LogLevel.Debug);
			return appHostBuilder;
		}

		// Public-API stand-in for MAUI's internal ApplicationDispatcher registration (see TestApplicationDispatcher.cs).
		static void AddTestApplicationDispatcher(this MauiAppBuilder appHostBuilder)
		{
			appHostBuilder.Services.TryAddSingleton<TestApplicationDispatcher>();
			appHostBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMauiInitializeService, TestApplicationDispatcher.Initializer>());
		}

		public static MauiAppBuilder UseVisualRunner(this MauiAppBuilder appHostBuilder)
		{
			appHostBuilder.UseMauiApp(svc => new MauiVisualRunnerApp(
				svc.GetRequiredService<TestOptions>(),
				svc.GetRequiredService<ILoggerFactory>().CreateLogger("TestRun")));

			return appHostBuilder;
		}

		public static MauiAppBuilder UseHeadlessRunner(this MauiAppBuilder appHostBuilder, HeadlessRunnerOptions options)
		{
			appHostBuilder.Services.AddSingleton(options);

#if __ANDROID__ || __IOS__ || MACCATALYST || WINDOWS
			appHostBuilder.Services.AddTransient(svc => new HeadlessTestRunner(
					svc.GetRequiredService<HeadlessRunnerOptions>(),
					svc.GetRequiredService<TestOptions>()));
#endif

#if WINDOWS
			// Also register the discovery/index-capable runner so a single-category run
			// ("App.exe <resultsFile> <categoryIndex>", e.g. how the Copilot review gate
			// verifies just the changed test category) works for ANY Windows device-test
			// app — not only Controls. HomePage resolves this runner solely when a
			// category-index CLI arg is supplied; the default full-suite run
			// ("App.exe <resultsFile>") still resolves HeadlessTestRunner, so the behavior
			// of the real device-test pipeline (which never passes a category index) is
			// unchanged. This lets the gate filter Core/Essentials/Graphics/BlazorWebView
			// Windows device tests instead of running the whole app (which can crash and
			// yield empty results, forcing an inconclusive gate).
			appHostBuilder.Services.AddTransient(svc => new ControlsHeadlessTestRunner(
					svc.GetRequiredService<HeadlessRunnerOptions>(),
					svc.GetRequiredService<TestOptions>()));
#endif

			appHostBuilder.Logging.AddConsole();

			return appHostBuilder;
		}

#if WINDOWS
		public static MauiAppBuilder UseControlsHeadlessRunner(this MauiAppBuilder appHostBuilder, HeadlessRunnerOptions options)
		{
			appHostBuilder.Services.AddSingleton(options);

			appHostBuilder.Services.AddTransient(svc => new ControlsHeadlessTestRunner(
					svc.GetRequiredService<HeadlessRunnerOptions>(),
					svc.GetRequiredService<TestOptions>()));

			return appHostBuilder;
		}
#endif
	}
}