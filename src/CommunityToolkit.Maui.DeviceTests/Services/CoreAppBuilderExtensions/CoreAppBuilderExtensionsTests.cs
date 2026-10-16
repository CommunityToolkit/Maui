#if !ANDROID
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.DependencyInjection;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

// Device tests of the `#if ANDROID` block of Core's AppBuilderExtensions.UseMauiCommunityToolkitCore. The option that turns the modal
// status bar support on is an app-wide static, so this class runs alone and restores it.
// - Android (CoreAppBuilderExtensionsTests.android.cs): the block registers the Android services and the OnCreate lifecycle event. The
//   tests of the completion event live there too, because on Android UseMauiCommunityToolkitCore runs the platform block inside the same
//   call, before it raises the event.
// - Every other platform (this file): the block is compiled out, which EnabledStatusBarOptionRegistersNothingOnThisPlatform checks. The
//   completion event itself is shared code that CommunityToolkit.Maui.UnitTests (Extensions/AppBuilderExtensionsTests) covers.
[Category(TestCategory.CoreAppBuilderExtensions)]
[Collection(TestCollections.DisableParallelization)]
#if ANDROID
public partial class CoreAppBuilderExtensionsTests : FragmentTestBase
#else
public partial class CoreAppBuilderExtensionsTests : CommunityToolkitHandlerTestBase
#endif
{
#if !ANDROID
	// The `#if ANDROID` block is the only thing the option adds to the app: elsewhere, the option on and the option off build the same
	// services (a lifecycle event registration is a service too), and the completion event is still raised.
	[Fact]
	public void EnabledStatusBarOptionRegistersNothingOnThisPlatform()
	{
		var snapshot = new StaticPropertySnapshot().Capture(typeof(Core.Options));
		var raisedCount = 0;
		void OnCompleted(object? sender, EventArgs e) => raisedCount++;
		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted += OnCompleted;

		try
		{
			var withOption = DescribeServices(MauiApp.CreateBuilder().UseMauiCommunityToolkitCore(options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true)));
			Assert.Equal(1, raisedCount);
			var withoutOption = DescribeServices(MauiApp.CreateBuilder().UseMauiCommunityToolkitCore(options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(false)));

			Assert.Equal(withoutOption, withOption);
			// The Android service does not even exist here (DialogFragmentService.android.cs).
			Assert.DoesNotContain(withOption, service => service.Contains("DialogFragmentService", StringComparison.Ordinal));
		}
		finally
		{
			Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= OnCompleted;
			snapshot.Restore();
		}
	}

	// Every registration of the builder as "service type => implementation", sorted (the registrations are compared as a multiset).
	static List<string> DescribeServices(MauiAppBuilder builder) => [.. builder.Services.Select(Describe).Order(StringComparer.Ordinal)];

	static string Describe(ServiceDescriptor descriptor)
	{
		var implementation = descriptor.IsKeyedService
			? descriptor.KeyedImplementationType?.FullName ?? descriptor.KeyedImplementationInstance?.GetType().FullName
			: descriptor.ImplementationType?.FullName ?? descriptor.ImplementationInstance?.GetType().FullName;

		return $"{descriptor.ServiceType.FullName}{(descriptor.IsKeyedService ? $" [{descriptor.ServiceKey}]" : string.Empty)} => {implementation ?? "factory"}";
	}
#endif
}