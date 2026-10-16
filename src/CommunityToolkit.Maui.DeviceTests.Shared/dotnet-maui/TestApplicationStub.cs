// Ported from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/Core/tests/DeviceTests.Shared/Stubs/CoreApplicationStub.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: renamed CoreApplicationStub -> TestApplicationStub, namespace CommunityToolkit.Maui.DeviceTests; CreateWindow
//                 throws instead of returning dotnet/maui's WindowStub (a test's MauiApp never creates platform windows).
#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.DeviceTests
{
	/// <summary>
	/// The <see cref="IApplication"/> of a test's own MauiApp. It is not a Controls Application, so creating it does not
	/// replace Application.Current (the runner's application).
	/// </summary>
	public class TestApplicationStub : IApplication
	{
		readonly List<IWindow> _windows = new List<IWindow>();

		public IElementHandler? Handler { get; set; }

		public IElement? Parent { get; set; }

		public IReadOnlyList<IWindow> Windows => _windows.AsReadOnly();

		public AppTheme UserAppTheme { get; set; }

		public IWindow CreateWindow(IActivationState? activationState) =>
			throw new NotSupportedException("A device test's MauiApp does not create platform windows: host views with AttachAndRun.");

		public void OpenWindow(IWindow window)
		{
			_windows.Add(window);
		}

		public void CloseWindow(IWindow window)
		{
			_windows.Remove(window);
		}

		public void ActivateWindow(IWindow window) { }

		public void ThemeChanged() { }
	}
}
