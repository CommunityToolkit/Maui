using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class MediaElementOptionsTests : BaseViewTest
{
	[Fact]
	public void MediaElementOptions_HasExpectedDefaults()
	{
		// Assert defaults on the options class
		MediaElementOptions.DefaultAndroidViewType.Should().Be(AndroidViewType.SurfaceView);
	}

	[Fact]
	public void SetDefaultAndroidForegroundServiceEnabled_Updates_StaticDefault()
	{
		var options = new MediaElementOptions();

		options.SetIsAndroidForegroundServiceEnabled(false);
		MediaElementOptions.IsAndroidForegroundServiceEnabled.Should().BeFalse();

		options.SetIsAndroidForegroundServiceEnabled(true);
		MediaElementOptions.IsAndroidForegroundServiceEnabled.Should().BeTrue();
	}

	[Fact]
	public void SetDefaultAndroidViewType_Updates_StaticDefault()
	{
		var options = new MediaElementOptions();

		options.SetDefaultAndroidViewType(AndroidViewType.TextureView);
		MediaElementOptions.DefaultAndroidViewType.Should().Be(AndroidViewType.TextureView);

		options.SetDefaultAndroidViewType(AndroidViewType.SurfaceView);
		MediaElementOptions.DefaultAndroidViewType.Should().Be(AndroidViewType.SurfaceView);
	}

	[Fact]
	public void InitializesFromMediaElementOptionsDefaults()
	{
		var options = new MediaElementOptions();

		// change defaults then create a new MediaElement and verify it picked them up
		options.SetDefaultAndroidViewType(AndroidViewType.TextureView);

		var mediaElement = new MediaElement();

		mediaElement.AndroidViewType.Should().Be(AndroidViewType.TextureView);
	}

	[Fact]
	public void MediaElementOptions_UpdateIsAndroidForegroundServiceEnabledWithUseMauiCommunityToolkitMediaElementParameterFalse_ShouldBeFalse()
	{
		var options = new MediaElementOptions();

		// change defaults then create a new MediaElement and verify it picked them up
		options.SetDefaultAndroidViewType(AndroidViewType.TextureView);
		options.SetIsAndroidForegroundServiceEnabled(false);

		var mediaElement = new MediaElement();

		mediaElement.AndroidViewType.Should().Be(AndroidViewType.TextureView);
		mediaElement.IsAndroidForegroundServiceEnabled.Should().BeFalse();
	}

	[Fact]
	public void MediaElementOptions_UpdateIsAndroidForegroundServiceEnabledWithUseMauiCommunityToolkitMediaElementParameterFalse_ShouldBeTrue()
	{
		var options = new MediaElementOptions();

		// change defaults then create a new MediaElement and verify it picked them up
		options.SetDefaultAndroidViewType(AndroidViewType.TextureView);
		options.SetIsAndroidForegroundServiceEnabled(true);

		var mediaElement = new MediaElement();

		mediaElement.AndroidViewType.Should().Be(AndroidViewType.TextureView);
		mediaElement.IsAndroidForegroundServiceEnabled.Should().BeTrue();
	}
	protected override void Dispose(bool isDisposing)
	{
		base.Dispose(isDisposing);

		var options = new MediaElementOptions();

		// restore original state
		options.SetDefaultAndroidViewType(MediaElementOptions.DefaultAndroidViewType);
		options.SetIsAndroidForegroundServiceEnabled(false);
	}
}