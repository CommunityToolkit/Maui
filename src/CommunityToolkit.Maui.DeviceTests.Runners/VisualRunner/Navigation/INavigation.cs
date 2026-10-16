// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/VisualRunner/Navigation/INavigation.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
#nullable enable
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.DeviceTests.Runners.VisualRunner
{
	public interface ITestNavigation
	{
		Task NavigateTo(PageType page, object? dataContext = null);
	}
}