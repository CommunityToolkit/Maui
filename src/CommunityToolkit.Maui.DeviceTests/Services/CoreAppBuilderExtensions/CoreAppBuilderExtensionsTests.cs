#if !ANDROID
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.DependencyInjection;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

// The option that turns the modal status bar support on is an app-wide static, so this class runs alone and restores it.
// The tests of the completion event live in the Android partial because there UseMauiCommunityToolkitCore runs the platform block inside the
// same call, before it raises the event. On every other platform the event is shared code that CommunityToolkit.Maui.UnitTests covers
// (Extensions/AppBuilderExtensionsTests).
[Category(TestCategory.CoreAppBuilderExtensions)]
[Collection(TestCollections.DisableParallelization)]
#if ANDROID
public partial class CoreAppBuilderExtensionsTests : FragmentTestBase
#else
public partial class CoreAppBuilderExtensionsTests : CommunityToolkitHandlerTestBase
#endif
{
#if !ANDROID
	// A lifecycle event registration is a service too, so comparing the services covers the whole `#if ANDROID` block.
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
			// The type exists only on Android (DialogFragmentService.android.cs), so it is matched by name.
			Assert.DoesNotContain(withOption, service => service.Contains("DialogFragmentService", StringComparison.Ordinal));
		}
		finally
		{
			Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= OnCompleted;
			snapshot.Restore();
		}
	}

	// Sorted, because the registrations are compared as a multiset.
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