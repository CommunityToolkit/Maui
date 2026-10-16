// Not part of dotnet/maui: public-API replacement for MAUI's internal Microsoft.Maui.Dispatching.ApplicationDispatcher,
// which the upstream TestDispatcher resolves. MAUI registers that type as a singleton and resolves it from an
// IMauiInitializeService during MauiAppBuilder.Build(), i.e. on the platform UI thread; this class does the same.
#nullable enable
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;

namespace CommunityToolkit.Maui.DeviceTests.Runners;

/// <summary>
/// Holds the dispatcher of the thread that built the runner's <see cref="MauiApp"/> (the platform UI thread).
/// </summary>
public sealed class TestApplicationDispatcher
{
	public TestApplicationDispatcher(IDispatcherProvider provider)
	{
		Dispatcher = provider.GetForCurrentThread()
			?? throw new InvalidOperationException("The MauiApp was not built on a thread that has a dispatcher (expected the platform UI thread).");
	}

	public IDispatcher Dispatcher { get; }

	// Resolving the scoped IDispatcher from TestServices.Services fails instead: that provider is the root provider and
	// MauiProgramDefaults enables ValidateScopes.
	internal sealed class Initializer : IMauiInitializeService
	{
		public void Initialize(IServiceProvider services) => _ = services.GetRequiredService<TestApplicationDispatcher>();
	}
}