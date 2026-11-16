// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests.Runners/VisualRunner/Utils/AsyncLock.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespaces renamed from Microsoft.Maui.TestUtils.DeviceTests.Runners* to CommunityToolkit.Maui.DeviceTests.Runners*.
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.DeviceTests.Runners.VisualRunner
{
	class AsyncLock
	{
		readonly SemaphoreSlim semaphore;
		readonly Task<Releaser> releaser;

		public AsyncLock()
		{
			semaphore = new SemaphoreSlim(1);
			releaser = Task.FromResult(new Releaser(this));
		}

		public struct Releaser : IDisposable
		{
			readonly AsyncLock toRelease;

			internal Releaser(AsyncLock toRelease)
			{
				this.toRelease = toRelease;
			}

			public void Dispose()
			{
				toRelease?.semaphore.Release();
			}
		}

#if DEBUG
		public Task<Releaser> LockAsync([CallerMemberName] string callingMethod = null, [CallerFilePath] string path = null, [CallerLineNumber] int line = 0)
		{
			Debug.WriteLine("AsyncLock.LockAsync called by: " + callingMethod + " in file: " + path + " : " + line);
#else
		public Task<Releaser> LockAsync()
		{
#endif
			var wait = semaphore.WaitAsync();

			return wait.IsCompleted ?
					   releaser :
					   wait.ContinueWith((_, state) => new Releaser((AsyncLock)state),
										 this, CancellationToken.None,
										 TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}
	}
}