// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/HeadlessRunner/Android/MauiTestActivity.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
// Toolkit change: moved from HeadlessRunner/Android to HeadlessRunner/android (toolkit platform-folder convention).
// Toolkit change: TaskCompletionSource uses RunContinuationsAsynchronously, so the instrumentation awaiting it never resumes inline on the test worker before the activity finishes.
#nullable enable
using System;
using System.Threading.Tasks;
using Android.OS;
using AndroidX.AppCompat.App;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner
{
	public abstract class MauiTestActivity : AppCompatActivity
	{
		public TaskCompletionSource<Bundle> TaskCompletionSource { get; } = new TaskCompletionSource<Bundle>(TaskCreationOptions.RunContinuationsAsynchronously);

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Do the work on the background thread to avoid a keyDispatchingTimedOut ANR
			Task.Run(async () =>
			{
				try
				{
					var runner = MauiTestInstrumentation.Current.Services.GetRequiredService<HeadlessTestRunner>();

					var bundle = await runner.RunTestsAsync();

					TaskCompletionSource.TrySetResult(bundle);
				}
				catch (Exception ex)
				{
					TaskCompletionSource.TrySetException(ex);
				}

				Finish();
			});
		}
	}
}