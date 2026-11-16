// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/HeadlessRunner/iOS/MauiTestViewController.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
// Toolkit change: moved from HeadlessRunner/iOS to HeadlessRunner/macios (iOS + Mac Catalyst, toolkit platform-folder convention).
#nullable enable
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner
{
	public class MauiTestViewController : UIViewController
	{
		Task? _task;

		public MauiTestViewController()
		{
		}

		public MauiTestViewController(Task task)
		{
			_task = task;
		}

		public override async void ViewDidLoad()
		{
			base.ViewDidLoad();

			if (_task is not null)
				await _task;

			var runner = MauiTestApplicationDelegate.Current.Services.GetRequiredService<HeadlessTestRunner>();

			await runner.RunTestsAsync();
		}
	}
}