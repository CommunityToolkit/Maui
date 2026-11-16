// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/VisualRunner/MauiVisualRunnerApp.xaml.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
#nullable enable
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.DeviceTests.Runners.VisualRunner.Pages;

namespace CommunityToolkit.Maui.DeviceTests.Runners.VisualRunner
{
	partial class MauiVisualRunnerApp : Application
	{
		readonly TestOptions _options;
		readonly ILogger _logger;

		public MauiVisualRunnerApp(TestOptions options, ILogger logger)
		{
			_options = options;
			_logger = logger;

			InitializeComponent();
		}

		protected override Window CreateWindow(IActivationState? activationState)
		{
			var hp = new HomePage();

			var nav = new TestNavigator(hp.Navigation);

			var runner = new DeviceRunner(_options.Assemblies, nav, _logger);

			var vm = new HomeViewModel(nav, runner);

			hp.BindingContext = vm;

			var navPage = new NavigationPage(hp);

			return new Window(navPage);
		}
	}
}