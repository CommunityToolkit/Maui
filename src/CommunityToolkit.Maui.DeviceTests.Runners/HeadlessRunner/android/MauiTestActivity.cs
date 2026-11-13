// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/HeadlessRunner/Android/MauiTestActivity.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
// Toolkit change: moved from HeadlessRunner/Android to HeadlessRunner/android (toolkit platform-folder convention).
// Toolkit change: TaskCompletionSource uses RunContinuationsAsynchronously, so the instrumentation awaiting it never resumes inline on the test worker before the activity finishes.
// Toolkit change: one test pass per process. Android recreates the activity for a configuration change that the generated ConfigurationChanges does not list (an overlay, locale or font-scale change), and OnCreate used to start a second pass next to the first.
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
		// One test pass per process. OnCreate runs on the main thread, so this needs no lock.
		static Task<Bundle>? testPass;

		public TaskCompletionSource<Bundle> TaskCompletionSource { get; } = new TaskCompletionSource<Bundle>(TaskCreationOptions.RunContinuationsAsynchronously);

		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			if (testPass is not null)
			{
				MauiTestInstrumentation.Current.OnTestActivityRecreated(this);
				Android.Util.Log.Warn("CtmDeviceTests", "The test activity was recreated; the running test pass continues in it.");
			}
			else
			{
				// Do the work on the background thread to avoid a keyDispatchingTimedOut ANR
				testPass = Task.Run(async () => await MauiTestInstrumentation.Current.Services.GetRequiredService<HeadlessTestRunner>().RunTestsAsync());
			}

			_ = CompleteWhenThePassEndsAsync(testPass);
		}

		async Task CompleteWhenThePassEndsAsync(Task<Bundle> pass)
		{
			try
			{
				TaskCompletionSource.TrySetResult(await pass);
			}
			catch (Exception ex)
			{
				TaskCompletionSource.TrySetException(ex);
			}

			Finish();
		}
	}
}