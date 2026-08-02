using System.Reflection;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Camera;

public class CameraFlashModeEnumTests
{
	[Theory]
	[InlineData(CameraFlashMode.Off, 0)]
	[InlineData(CameraFlashMode.On, 1)]
	[InlineData(CameraFlashMode.Auto, 2)]
	public void CameraFlashMode_HasExpectedValues(CameraFlashMode mode, int expected)
	{
		Assert.Equal(expected, (int)mode);
	}
}

public class CameraPositionEnumTests
{
	[Theory]
	[InlineData(CameraPosition.Unknown, 0)]
	[InlineData(CameraPosition.Rear, 1)]
	[InlineData(CameraPosition.Front, 2)]
	public void CameraPosition_HasExpectedValues(CameraPosition position, int expected)
	{
		Assert.Equal(expected, (int)position);
	}
}

public class CameraViewDefaultsTests
{
	static readonly Type cameraViewDefaultsType = typeof(CameraView).Assembly
		.GetType("CommunityToolkit.Maui.Core.CameraViewDefaults")
		?? throw new InvalidOperationException("CameraViewDefaults type not found");

	[Fact]
	public void CameraViewDefaults_CameraFlashMode_IsOff()
	{
		var property = cameraViewDefaultsType.GetProperty("CameraFlashMode", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(property);

		var value = property.GetValue(null);
		Assert.Equal(CameraFlashMode.Off, value);
	}

	[Fact]
	public void CameraViewDefaults_IsTorchOn_IsFalse()
	{
		var field = cameraViewDefaultsType.GetField("IsTorchOn", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);

		var value = field.GetValue(null);
		Assert.Equal(false, value);
	}

	[Fact]
	public void CameraViewDefaults_ZoomFactor_IsOne()
	{
		var field = cameraViewDefaultsType.GetField("ZoomFactor", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);

		var value = field.GetValue(null);
		Assert.Equal(1.0f, value);
	}

	[Fact]
	public void CameraViewDefaults_IsAvailable_IsFalse()
	{
		var field = cameraViewDefaultsType.GetField("IsAvailable", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);

		var value = field.GetValue(null);
		Assert.Equal(false, value);
	}

	[Fact]
	public void CameraViewDefaults_IsCameraBusy_IsFalse()
	{
		var field = cameraViewDefaultsType.GetField("IsCameraBusy", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(field);

		var value = field.GetValue(null);
		Assert.Equal(false, value);
	}

	[Fact]
	public void CameraViewDefaults_ImageCaptureResolution_IsZero()
	{
		var property = cameraViewDefaultsType.GetProperty("ImageCaptureResolution", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(property);

		var value = (Size)(property.GetValue(null) ?? Size.Zero);
		Assert.Equal(Size.Zero, value);
	}
}

public class CameraExceptionTests
{
	[Fact]
	public void CameraException_HasMessage()
	{
		var exception = new CameraException("Camera unavailable");

		Assert.Equal("Camera unavailable", exception.Message);
	}
}

public class CameraInfoTests
{
	/// <summary>
	/// CameraInfo has platform-specific constructor parameters (e.g. MediaFrameSourceGroup on Windows)
	/// that require real camera hardware to instantiate. Skip these tests in CI/headless environments.
	/// </summary>
	static CameraInfo CreateCameraInfo(string name, string deviceId, CameraPosition position, bool isFlashSupported, float minZoom, float maxZoom, IEnumerable<Size> resolutions)
	{
		var cameraInfoType = typeof(CameraInfo);
		var constructors = cameraInfoType.GetConstructors();
		Assert.Single(constructors);

		var parameters = constructors[0].GetParameters();
		var args = new object?[parameters.Length];
		args[0] = name;
		args[1] = deviceId;
		args[2] = position;
		args[3] = isFlashSupported;
		args[4] = minZoom;
		args[5] = maxZoom;
		args[6] = resolutions;

		// Fill platform-specific parameters with default values
		for (var i = 7; i < parameters.Length; i++)
		{
			args[i] = parameters[i].ParameterType.IsValueType
				? Activator.CreateInstance(parameters[i].ParameterType)
				: null;
		}

		return (CameraInfo)constructors[0].Invoke(args);
	}

	[Fact(Skip = "Requires platform-specific camera hardware (MediaFrameSourceGroup on Windows)")]
	public void CameraInfo_CanBeCreated()
	{
		var cameraInfo = CreateCameraInfo(
			"Test Camera", "device-123", CameraPosition.Rear, true, 1.0f, 5.0f,
			[new Size(1920, 1080), new Size(1280, 720)]);

		Assert.Equal("Test Camera", cameraInfo.Name);
		Assert.Equal("device-123", cameraInfo.DeviceId);
		Assert.Equal(CameraPosition.Rear, cameraInfo.Position);
		Assert.True(cameraInfo.IsFlashSupported);
		Assert.Equal(1.0f, cameraInfo.MinimumZoomFactor);
		Assert.Equal(5.0f, cameraInfo.MaximumZoomFactor);
		Assert.Equal(2, cameraInfo.SupportedResolutions.Count);
	}

	[Fact(Skip = "Requires platform-specific camera hardware (MediaFrameSourceGroup on Windows)")]
	public void CameraInfo_FrontCamera()
	{
		var cameraInfo = CreateCameraInfo(
			"Front Camera", "front-456", CameraPosition.Front, false, 1.0f, 2.0f,
			[new Size(640, 480)]);

		Assert.Equal(CameraPosition.Front, cameraInfo.Position);
		Assert.False(cameraInfo.IsFlashSupported);
	}

	[Fact(Skip = "Requires platform-specific camera hardware (MediaFrameSourceGroup on Windows)")]
	public void CameraInfo_Equality()
	{
		var camera1 = CreateCameraInfo("Cam", "id1", CameraPosition.Rear, true, 1.0f, 5.0f, [new Size(1920, 1080)]);
		var camera2 = CreateCameraInfo("Cam", "id1", CameraPosition.Rear, true, 1.0f, 5.0f, [new Size(1920, 1080)]);

		Assert.Equal(camera1, camera2);
	}

	[Fact(Skip = "Requires platform-specific camera hardware (MediaFrameSourceGroup on Windows)")]
	public void CameraInfo_Inequality()
	{
		var camera1 = CreateCameraInfo("Cam1", "id1", CameraPosition.Rear, true, 1.0f, 5.0f, [new Size(1920, 1080)]);
		var camera2 = CreateCameraInfo("Cam2", "id2", CameraPosition.Front, false, 1.0f, 2.0f, [new Size(640, 480)]);

		Assert.NotEqual(camera1, camera2);
	}
}

public class MediaCapturedEventArgsTests
{
	[Fact]
	public void MediaCapturedEventArgs_CarriesStream()
	{
		using var stream = new MemoryStream([1, 2, 3]);
		var args = new MediaCapturedEventArgs(stream);

		Assert.NotNull(args.Media);
		Assert.Equal(3, args.Media.Length);
	}
}

public class MediaCaptureFailedEventArgsTests
{
	[Fact]
	public void MediaCaptureFailedEventArgs_CarriesFailureReason()
	{
		var args = new MediaCaptureFailedEventArgs("Camera disconnected");

		Assert.Equal("Camera disconnected", args.FailureReason);
	}
}

public class CameraViewBindablePropertyTests
{
	[Fact]
	public void CameraView_DefaultFlashMode()
	{
		var cameraView = new CameraView();

		Assert.Equal(CameraFlashMode.Off, cameraView.CameraFlashMode);
	}

	[Fact]
	public void CameraView_DefaultIsTorchOn()
	{
		var cameraView = new CameraView();

		Assert.False(cameraView.IsTorchOn);
	}

	[Fact]
	public void CameraView_DefaultZoomFactor()
	{
		var cameraView = new CameraView();

		Assert.Equal(1.0f, cameraView.ZoomFactor);
	}

	[Fact]
	public void CameraView_DefaultIsAvailable()
	{
		var cameraView = new CameraView();

		Assert.False(cameraView.IsAvailable);
	}

	[Fact]
	public void CameraView_DefaultIsBusy()
	{
		var cameraView = new CameraView();

		Assert.False(cameraView.IsBusy);
	}

	[Fact]
	public void CameraView_CanSetFlashMode()
	{
		var cameraView = new CameraView
		{
			CameraFlashMode = CameraFlashMode.Auto
		};

		Assert.Equal(CameraFlashMode.Auto, cameraView.CameraFlashMode);
	}

	[Fact]
	public void CameraView_CanSetZoomFactor()
	{
		var cameraView = new CameraView
		{
			ZoomFactor = 3.5f
		};

		Assert.Equal(3.5f, cameraView.ZoomFactor);
	}

	[Fact]
	public void CameraView_CanSetIsTorchOn()
	{
		var cameraView = new CameraView
		{
			IsTorchOn = true
		};

		Assert.True(cameraView.IsTorchOn);
	}
}
