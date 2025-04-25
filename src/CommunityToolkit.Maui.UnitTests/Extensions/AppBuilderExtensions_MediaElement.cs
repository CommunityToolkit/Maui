using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;
public class AppBuilderExtensions_MediaElement
{
	[Fact]
    public void UseMauiCommunityToolkitMediaElement_ShouldSetDefaultAndroidViewType()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiCommunityToolkitMediaElement(options =>
        {
            options.SetDefaultAndroidViewType(AndroidViewType.SurfaceView);
        });

        MediaElementOptions.DefaultAndroidViewType.Should().Be(AndroidViewType.SurfaceView);
    }
	[Fact]
	public void UseMauiCommunityToolkitMediaElement_ShouldAllowSettingDefaultAndroidViewTypeAfterNullOptions()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiCommunityToolkitMediaElement(null!);
		var mediaElementOptions = new MediaElementOptions(builder);
		mediaElementOptions.SetDefaultAndroidViewType(AndroidViewType.SurfaceView);
		MediaElementOptions.DefaultAndroidViewType.Should().Be(AndroidViewType.SurfaceView);
	}
	[Fact]
	public void UseMauiCommunityToolkitMediaElement_ShouldAllowChangingDefaultAndroidViewType()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiCommunityToolkitMediaElement();
		var mediaElementOptions = new MediaElementOptions(builder);
		mediaElementOptions.SetDefaultAndroidViewType(AndroidViewType.TextureView);
		MediaElementOptions.DefaultAndroidViewType.Should().Be(AndroidViewType.TextureView);
	}
}
