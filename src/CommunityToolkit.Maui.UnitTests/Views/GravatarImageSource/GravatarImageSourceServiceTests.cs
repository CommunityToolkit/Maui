using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.GravatarImageSource;

public class GravatarImageSourceServiceTests : BaseTest
{
	[Fact]
	public void ResolvesToConcreteTypeOverInterface()
	{
		var provider = CreateImageSourceServiceProvider(services => _ = services.AddService<IGravatarImageSource, GravatarImageSourceService>());
		var service = provider.GetRequiredImageSourceService(new GravatarImageImageSourceStub());
		Assert.IsType<GravatarImageSourceService>(service);
	}

	[Fact]
	public void CanResolveCorrectServiceWhenMultiple()
	{
		var provider = CreateImageSourceServiceProvider(services =>
		{
			services.AddService<IGravatarImageSource, GravatarImageSourceService>();
			services.AddService<IUriImageSource, UriImageSourceService>();
		});

		var service = provider.GetRequiredImageSourceService(new GravatarImageImageSourceStub());
		Assert.IsType<GravatarImageSourceService>(service);

		service = provider.GetRequiredImageSourceService(new UriImageSourceStub());
		Assert.IsType<UriImageSourceService>(service);
	}

	static IImageSourceServiceProvider CreateImageSourceServiceProvider(Action<IImageSourceServiceCollection> configure)
	{
		var mauiApp = MauiApp.CreateBuilder()
			.ConfigureImageSources(configure)
			.Build();
		return mauiApp.Services.GetRequiredService<IImageSourceServiceProvider>();
	}
}