using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

#pragma warning disable CS8625 // The tests pass null to parameters that are not nullable on purpose
#pragma warning disable CS8604
public class CameraNullGuardTests : BaseViewTest
{
	[Fact]
	public void UseMauiCommunityToolkitCamera_NullBuilder_Throws()
	{
		MauiAppBuilder? builder = null;

		// Both the Camera and the MediaElement assemblies declare an AppBuilderExtensions class, so the extension method is called on the instance
		var exception = Assert.Throws<ArgumentNullException>(() => builder.UseMauiCommunityToolkitCamera());

		Assert.Equal("builder", exception.ParamName);
	}

	[Fact]
	public void ErrorOccurredEventArgs_NullException_Throws()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => new ErrorOccurredEventArgs(null));

		Assert.Equal("ex", exception.ParamName);
	}

	[Fact]
	public void MediaCapturedEventArgs_NullStream_Throws()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => new MediaCapturedEventArgs(null));

		Assert.Equal("stream", exception.ParamName);
	}

	[Fact]
	public void MediaCaptureFailedEventArgs_NullFailureReason_Throws()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => new MediaCaptureFailedEventArgs(null));

		Assert.Equal("failureReason", exception.ParamName);
	}

	[Fact]
	public void EventArgs_NonNullArguments_ExposeThem()
	{
		var exception = new InvalidOperationException();
		using var stream = new MemoryStream();

		Assert.Same(exception, new ErrorOccurredEventArgs(exception).Exception);
		Assert.Same(stream, new MediaCapturedEventArgs(stream).Media);
		Assert.Equal("reason", new MediaCaptureFailedEventArgs("reason").FailureReason);
	}

	[Fact]
	public void CameraInfo_NullName_Throws()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => new CameraInfo(null, "0", CameraPosition.Rear, true, 1.0f, 4.0f, []));

		Assert.Equal("name", exception.ParamName);
	}

	[Fact]
	public void CameraInfo_NullDeviceId_Throws()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => new CameraInfo("Back Camera", null, CameraPosition.Rear, true, 1.0f, 4.0f, []));

		Assert.Equal("deviceId", exception.ParamName);
	}

	[Fact]
	public void CameraInfo_NullSupportedResolutions_Throws()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => new CameraInfo("Back Camera", "0", CameraPosition.Rear, true, 1.0f, 4.0f, null));

		Assert.Equal("supportedResolutions", exception.ParamName);
	}

	[Fact]
	public void StartVideoRecording_NullStream_ThrowsBeforeTheHandlerIsLookedUp()
	{
		var cameraViewWithoutHandler = new CameraView();

		var exception = Assert.Throws<ArgumentNullException>(() =>
		{
			// Discarded without awaiting: the exception must come from the call itself, not from the returned Task
			_ = cameraViewWithoutHandler.StartVideoRecording(null, CancellationToken.None);
		});

		Assert.Equal("stream", exception.ParamName);
	}

	[Fact]
	public void ConnectHandler_NullPlatformView_ThrowsToTheCaller()
	{
		var handler = new ExposedCameraViewHandler();

		var exception = Assert.Throws<ArgumentNullException>(() => handler.Connect(null));

		Assert.Equal("platformView", exception.ParamName);
	}

	[Fact]
	public void DisconnectHandler_NullPlatformView_Throws()
	{
		var handler = new ExposedCameraViewHandler();

		var exception = Assert.Throws<ArgumentNullException>(() => handler.Disconnect(null));

		Assert.Equal("platformView", exception.ParamName);
	}

	sealed class ExposedCameraViewHandler : CameraViewHandler
	{
		public void Connect(IView platformView) => ConnectHandler(platformView);

		public void Disconnect(IView platformView) => DisconnectHandler(platformView);
	}
}