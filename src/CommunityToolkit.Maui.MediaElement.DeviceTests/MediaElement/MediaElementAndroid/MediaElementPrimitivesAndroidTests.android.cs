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
	public void MediaControlsServiceIsRegisteredAsASingletonService()
	{
		IServiceCollection? services = null;
		SetupProbeBuilder(configureBuilder: builder => services = builder.Services);

		// The container is not asked for the service: a MediaControlsService made by the container would be a service object that the
		// system never attached.
		var descriptor = Assert.Single(services!, registration => registration.ServiceType == typeof(MediaControlsService));
		Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
		Assert.Equal(typeof(MediaControlsService), descriptor.ImplementationType);
	}

	[Fact]
	public void MediaElementHandlerIsRegisteredForMediaElement()
	{
		SetupBuilder();

		var handlerType = MauiContext.Handlers.GetHandlerType(typeof(CommunityToolkit.Maui.Views.MediaElement));

		Assert.Equal(typeof(CommunityToolkit.Maui.Core.Handlers.MediaElementHandler), handlerType);
	}
}