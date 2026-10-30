// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/HeadlessRunner/MacCatalyst/MauiTestSceneDelegate.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
// Toolkit change: moved from HeadlessRunner/MacCatalyst to HeadlessRunner/macios: iOS apps need the scene delegate too (iOS 27 SDK UIScene requirement).
#nullable enable
using System;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner;

/// <summary>
/// Attaches the headless runner's window to its Mac Catalyst scene before starting tests.
/// </summary>
public class MauiTestSceneDelegate : MauiUISceneDelegate
{
	public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
	{
		if (UIApplication.SharedApplication.Delegate is not MauiTestApplicationDelegate applicationDelegate)
		{
			base.WillConnect(scene, session, connectionOptions);
			return;
		}

		if (scene is not UIWindowScene windowScene)
			throw new InvalidOperationException("Headless device tests require a window scene.");

		var headlessWindow = applicationDelegate.Window
			?? throw new InvalidOperationException("The headless test window must be created before connecting its scene.");

		var previousScene = headlessWindow.WindowScene;
		if (previousScene is not null &&
			previousScene.Handle != windowScene.Handle &&
			previousScene.ActivationState != UISceneActivationState.Unattached)
			throw new InvalidOperationException("The headless test window already belongs to another connected scene.");

		headlessWindow.WindowScene = windowScene;
		Window = headlessWindow;
		headlessWindow.MakeKeyAndVisible();
		applicationDelegate.SetWindowReady();
	}
}
