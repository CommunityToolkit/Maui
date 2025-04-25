using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class MediaElementOptionsTests
{
    [Fact]
    public void DefaultAndroidViewType_ShouldBe_SurfaceView()
    {
        MediaElementOptions.DefaultAndroidViewType.Should().Be(AndroidViewType.SurfaceView);
    }

    [Fact]
    public void SetDefaultAndroidViewType_ShouldUpdateDefaultAndroidViewType()
    {
        var mediaElementOptions = new MediaElementOptions(null!);
        mediaElementOptions.SetDefaultAndroidViewType(AndroidViewType.TextureView);
        MediaElementOptions.DefaultAndroidViewType.Should().Be(AndroidViewType.TextureView);
    }

    [Fact]
    public void SetDefaultAndroidViewType_ShouldNotThrow_WhenCalledWithValidValue()
    {
        var mediaElementOptions = new MediaElementOptions(null!);
        Action act = () => mediaElementOptions.SetDefaultAndroidViewType(AndroidViewType.SurfaceView);
        act.Should().NotThrow();
    }
}
