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
		MediaElementOptions.IsAndroidForegroundServiceEnabled.Should().BeTrue();
	}

	[Fact]
	public void SetDefaultAndroidForegroundServiceEnabled_Updates_StaticDefault()
	{
		var optionsInstance = (MediaElementOptions)Activator.CreateInstance(typeof(MediaElementOptions), nonPublic: true)!;
		var original = MediaElementOptions.IsAndroidForegroundServiceEnabled;

		try
		{
			optionsInstance.SetDefaultAndroidForegroundServiceEnabled(false);
			MediaElementOptions.IsAndroidForegroundServiceEnabled.Should().BeFalse();

			optionsInstance.SetDefaultAndroidForegroundServiceEnabled(true);
			MediaElementOptions.IsAndroidForegroundServiceEnabled.Should().BeTrue();
		}
		finally
		{
			// restore original state to avoid test pollution
			optionsInstance.SetDefaultAndroidForegroundServiceEnabled(original);
		}
	}

	[Fact]
	public void SetDefaultAndroidViewType_Updates_StaticDefault()
	{
		var optionsInstance = (MediaElementOptions)Activator.CreateInstance(typeof(MediaElementOptions), nonPublic: true)!;
		var original = MediaElementOptions.DefaultAndroidViewType;

		try
		{
			optionsInstance.SetDefaultAndroidViewType(AndroidViewType.TextureView);
			MediaElementOptions.DefaultAndroidViewType.Should().Be(AndroidViewType.TextureView);

			optionsInstance.SetDefaultAndroidViewType(AndroidViewType.SurfaceView);
			MediaElementOptions.DefaultAndroidViewType.Should().Be(AndroidViewType.SurfaceView);
		}
		finally
		{
			// restore original state to avoid test pollution
			optionsInstance.SetDefaultAndroidViewType(original);
		}
	}

	[Fact]
	public void MediaElement_Initializes_From_MediaElementOptions_Defaults()
	{
		var optionsInstance = (MediaElementOptions)Activator.CreateInstance(typeof(MediaElementOptions), nonPublic: true)!;
		var originalViewType = MediaElementOptions.DefaultAndroidViewType;
		var originalForegroundEnabled = MediaElementOptions.IsAndroidForegroundServiceEnabled;

		try
		{
			// change defaults then create a new MediaElement and verify it picked them up
			optionsInstance.SetDefaultAndroidViewType(AndroidViewType.TextureView);
			optionsInstance.SetDefaultAndroidForegroundServiceEnabled(false);

			var mediaElement = new MediaElement();

			mediaElement.AndroidViewType.Should().Be(AndroidViewType.TextureView);
			mediaElement.IsAndroidForegroundServiceEnabled.Should().BeFalse();
		}
		finally
		{
			// restore original state
			optionsInstance.SetDefaultAndroidViewType(originalViewType);
			optionsInstance.SetDefaultAndroidForegroundServiceEnabled(originalForegroundEnabled);
		}
	}
}