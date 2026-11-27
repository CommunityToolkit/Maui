using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Media.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MediaElementAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class MediaElementPrimitivesAndroidTests : MediaElementAndroidTestBase
{
	[Fact]
	public async Task PlayerViewChangedEventArgsCarryThePlayerView()
	{
		SetupProbeBuilder();
		var context = MauiContext.Context!;

		await InvokeOnMainThreadAsync(() =>
		{
			using var playerView = new PlayerView(context);

			var args = new PlayerViewChangedEventArgs(playerView);

			Assert.Same(playerView, args.UpdatedPlayerView);
			Assert.IsType<EventArgs>(args, exactMatch: false);
		});
	}

	[Fact]
	public void PlayerViewChangedEventArgsWithoutAPlayerViewThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // The test passes null to a parameter that is not nullable on purpose
		var exception = Assert.Throws<ArgumentNullException>(() => new PlayerViewChangedEventArgs(null));
#pragma warning restore CS8625

		Assert.Equal("playerView", exception.ParamName);
	}

	[Fact]
	public void MediaControlsServiceIsNotRegisteredInTheContainer()
	{
		IServiceCollection? services = null;
		SetupProbeBuilder(configureBuilder: builder => services = builder.Services);

		// The container is not asked for the service: the system creates and attaches it, and one that the container made would be a
		// second service object that nothing attached.
		Assert.DoesNotContain(services!, registration => registration.ServiceType == typeof(MediaControlsService) || registration.ImplementationType == typeof(MediaControlsService));
	}

	[Fact]
	public void MediaElementHandlerIsRegisteredForMediaElement()
	{
		SetupBuilder();

		var handlerType = MauiContext.Handlers.GetHandlerType(typeof(CommunityToolkit.Maui.Views.MediaElement));

		Assert.Equal(typeof(CommunityToolkit.Maui.Core.Handlers.MediaElementHandler), handlerType);
	}
}