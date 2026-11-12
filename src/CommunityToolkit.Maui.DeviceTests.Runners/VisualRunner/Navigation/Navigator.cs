// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/VisualRunner/Navigation/Navigator.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.DeviceTests.Runners.VisualRunner.Pages;

namespace CommunityToolkit.Maui.DeviceTests.Runners.VisualRunner
{
	class TestNavigator : ITestNavigation
	{
		readonly INavigation _navigation;

		public TestNavigator(INavigation navigation)
		{
			_navigation = navigation;
		}

		public Task NavigateTo(PageType page, object? dataContext = null)
		{
			ContentPage p = page switch
			{
				PageType.Home => new HomePage(),
				PageType.AssemblyTestList => new TestAssemblyPage(),
				PageType.TestResult => new TestResultPage(),
				PageType.Credits => new CreditsPage(),
				_ => throw new ArgumentOutOfRangeException(nameof(page)),
			};

			p.BindingContext = dataContext;

			return _navigation.PushAsync(p);
		}
	}
}