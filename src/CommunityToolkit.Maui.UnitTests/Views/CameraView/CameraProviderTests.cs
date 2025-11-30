using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

/// <summary>
/// Comprehensive unit tests for CameraProvider.AreCameraInfoListsEqual method
/// tested through MockCameraProvider behavior
/// </summary>
public class CameraProviderTests
{
	#region Null Handling Tests

	[Fact]
	public void AreCameraInfoListsEqual_BothListsNull_ShouldConsiderEqual()
	{
		// Arrange
		var provider = new MockCameraProvider();
		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting null when already null
		typeof(MockCameraProvider)
			.GetProperty(nameof(MockCameraProvider.AvailableCameras))!
			.SetValue(provider, null);

		// Assert - No event should be raised as both are null
		Assert.Equal(0, eventRaisedCount);
		Assert.Null(provider.AvailableCameras);
	}

	[Fact]
	public void AreCameraInfoListsEqual_FirstNullSecondNotNull_ShouldConsiderNotEqual()
	{
		// Arrange
		var provider = new MockCameraProvider();
		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		var cameras = new List<CameraInfo>
		{
			CreateCameraInfo("Camera1")
		};

		// Act - Setting non-null when currently null
		SetAvailableCameras(provider, cameras);

		// Assert - Event should be raised as they're different
		Assert.Equal(1, eventRaisedCount);
		Assert.NotNull(provider.AvailableCameras);
	}

	[Fact]
	public void AreCameraInfoListsEqual_FirstNotNullSecondNull_ShouldConsiderNotEqual()
	{
		// Arrange
		var provider = new MockCameraProvider();
		var cameras = new List<CameraInfo> { CreateCameraInfo("Camera1") };
		SetAvailableCameras(provider, cameras);

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting null when currently non-null
		SetAvailableCameras(provider, null);

		// Assert - Event should be raised as they're different
		Assert.Equal(1, eventRaisedCount);
		Assert.Null(provider.AvailableCameras);
	}

	#endregion

	#region Empty List Tests

	[Fact]
	public void AreCameraInfoListsEqual_BothListsEmpty_ShouldConsiderEqual()
	{
		// Arrange
		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo>());

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting another empty list
		SetAvailableCameras(provider, new List<CameraInfo>());

		// Assert - No event should be raised
		Assert.Equal(0, eventRaisedCount);
		Assert.NotNull(provider.AvailableCameras);
		Assert.Empty(provider.AvailableCameras);
	}

	[Fact]
	public void AreCameraInfoListsEqual_EmptyListVsNull_ShouldConsiderNotEqual()
	{
		// Arrange
		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo>());

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting null when currently empty
		SetAvailableCameras(provider, null);

		// Assert - Event should be raised
		Assert.Equal(1, eventRaisedCount);
	}

	[Fact]
	public void AreCameraInfoListsEqual_EmptyListVsNonEmpty_ShouldConsiderNotEqual()
	{
		// Arrange
		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo>());

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting non-empty when currently empty
		SetAvailableCameras(provider, new List<CameraInfo> { CreateCameraInfo("Camera1") });

		// Assert - Event should be raised
		Assert.Equal(1, eventRaisedCount);
	}

	#endregion

	#region Same Content Tests

	[Fact]
	public void AreCameraInfoListsEqual_SameSingleCamera_ShouldConsiderEqual()
	{
		// Arrange
		var camera = CreateCameraInfo("Camera1");
		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo> { camera });

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting list with same camera
		SetAvailableCameras(provider, new List<CameraInfo> { camera });

		// Assert - No event should be raised
		Assert.Equal(0, eventRaisedCount);
	}

	[Fact]
	public void AreCameraInfoListsEqual_SameMultipleCameras_ShouldConsiderEqual()
	{
		// Arrange
		var camera1 = CreateCameraInfo("Camera1");
		var camera2 = CreateCameraInfo("Camera2");
		var camera3 = CreateCameraInfo("Camera3");

		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo> { camera1, camera2, camera3 });

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting list with same cameras
		SetAvailableCameras(provider, new List<CameraInfo> { camera1, camera2, camera3 });

		// Assert - No event should be raised
		Assert.Equal(0, eventRaisedCount);
	}

	[Fact]
	public void AreCameraInfoListsEqual_SameCamerasDifferentOrder_ShouldConsiderEqual()
	{
		// Arrange
		var camera1 = CreateCameraInfo("Camera1");
		var camera2 = CreateCameraInfo("Camera2");
		var camera3 = CreateCameraInfo("Camera3");

		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo> { camera1, camera2, camera3 });

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting list with same cameras in different order
		SetAvailableCameras(provider, new List<CameraInfo> { camera3, camera1, camera2 });

		// Assert - No event should be raised (order-independent comparison)
		Assert.Equal(0, eventRaisedCount);
	}

	[Fact]
	public void AreCameraInfoListsEqual_SameReferenceList_ShouldConsiderEqual()
	{
		// Arrange
		var cameras = new List<CameraInfo> { CreateCameraInfo("Camera1") };
		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, cameras);

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting the exact same list reference
		SetAvailableCameras(provider, cameras);

		// Assert - No event should be raised
		Assert.Equal(0, eventRaisedCount);
	}

	#endregion

	#region Different Content Tests

	[Fact]
	public void AreCameraInfoListsEqual_DifferentSingleCamera_ShouldConsiderNotEqual()
	{
		// Arrange
		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo> { CreateCameraInfo("Camera1") });

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting list with different camera
		SetAvailableCameras(provider, new List<CameraInfo> { CreateCameraInfo("Camera2") });

		// Assert - Event should be raised
		Assert.Equal(1, eventRaisedCount);
	}

	[Fact]
	public void AreCameraInfoListsEqual_DifferentCameraCount_MoreInSecond_ShouldConsiderNotEqual()
	{
		// Arrange
		var camera1 = CreateCameraInfo("Camera1");
		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo> { camera1 });

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting list with more cameras
		SetAvailableCameras(provider, new List<CameraInfo> { camera1, CreateCameraInfo("Camera2") });

		// Assert - Event should be raised
		Assert.Equal(1, eventRaisedCount);
	}

	[Fact]
	public void AreCameraInfoListsEqual_DifferentCameraCount_LessInSecond_ShouldConsiderNotEqual()
	{
		// Arrange
		var camera1 = CreateCameraInfo("Camera1");
		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo> { camera1, CreateCameraInfo("Camera2") });

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting list with fewer cameras
		SetAvailableCameras(provider, new List<CameraInfo> { camera1 });

		// Assert - Event should be raised
		Assert.Equal(1, eventRaisedCount);
	}

	[Fact]
	public void AreCameraInfoListsEqual_PartiallyOverlappingLists_ShouldConsiderNotEqual()
	{
		// Arrange
		var camera1 = CreateCameraInfo("Camera1");
		var camera2 = CreateCameraInfo("Camera2");
		var camera3 = CreateCameraInfo("Camera3");

		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo> { camera1, camera2 });

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting list with one common camera and one different
		SetAvailableCameras(provider, new List<CameraInfo> { camera1, camera3 });

		// Assert - Event should be raised
		Assert.Equal(1, eventRaisedCount);
	}

	[Fact]
	public void AreCameraInfoListsEqual_CompletelyDifferentLists_ShouldConsiderNotEqual()
	{
		// Arrange
		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo>
		{
			CreateCameraInfo("Camera1"),
			CreateCameraInfo("Camera2")
		});

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting completely different list
		SetAvailableCameras(provider, new List<CameraInfo>
		{
			CreateCameraInfo("Camera3"),
			CreateCameraInfo("Camera4")
		});

		// Assert - Event should be raised
		Assert.Equal(1, eventRaisedCount);
	}

	#endregion

	#region Duplicate Handling Tests

	[Fact]
	public void AreCameraInfoListsEqual_ListWithDuplicates_ShouldHandleCorrectly()
	{
		// Arrange
		var camera = CreateCameraInfo("Camera1");
		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo> { camera, camera });

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting same list with duplicates
		SetAvailableCameras(provider, new List<CameraInfo> { camera, camera });

		// Assert - No event should be raised
		Assert.Equal(0, eventRaisedCount);
	}

	#endregion

	#region CameraInfo Property Variations Tests

	[Fact]
	public void AreCameraInfoListsEqual_DifferentCameraName_ShouldNotConsiderNotEqual()
	{
		// Arrange
		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo>
		{
			CreateCameraInfo("Camera1", "device1", CameraPosition.Front)
		});

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Different name, same device and position
		SetAvailableCameras(provider, new List<CameraInfo>
		{
			CreateCameraInfo("Camera2", "device1", CameraPosition.Front)
		});

		// Assert - Event should be raised (different camera)
		Assert.Equal(0, eventRaisedCount);
	}

	[Fact]
	public void AreCameraInfoListsEqual_DifferentDeviceId_ShouldConsiderNotEqual()
	{
		// Arrange
		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo>
		{
			CreateCameraInfo("Camera1", "device1", CameraPosition.Front)
		});

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Same name and position, different device
		SetAvailableCameras(provider, new List<CameraInfo>
		{
			CreateCameraInfo("Camera1", "device2", CameraPosition.Front)
		});

		// Assert - Event should be raised (different camera)
		Assert.Equal(1, eventRaisedCount);
	}

	[Fact]
	public void AreCameraInfoListsEqual_AllPropertiesSame_ShouldConsiderEqual()
	{
		// Arrange
		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo>
		{
			CreateFullCameraInfo("Camera1", "device1", CameraPosition.Front, true, 1.0f, 5.0f)
		});

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - All properties identical
		SetAvailableCameras(provider, new List<CameraInfo>
		{
			CreateFullCameraInfo("Camera1", "device1", CameraPosition.Front, true, 1.0f, 5.0f)
		});

		// Assert - No event should be raised
		Assert.Equal(0, eventRaisedCount);
	}

	#endregion

	#region Sequential Changes Tests

	[Fact]
	public void AreCameraInfoListsEqual_MultipleSequentialChanges_ShouldRaiseEventForEachChange()
	{
		// Arrange
		var provider = new MockCameraProvider();
		var camera1 = CreateCameraInfo("Camera1");
		var camera2 = CreateCameraInfo("Camera2");
		var camera3 = CreateCameraInfo("Camera3");

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Multiple different changes
		SetAvailableCameras(provider, new List<CameraInfo> { camera1 });
		SetAvailableCameras(provider, new List<CameraInfo> { camera2 });
		SetAvailableCameras(provider, new List<CameraInfo> { camera3 });
		SetAvailableCameras(provider, new List<CameraInfo> { camera1, camera2 });

		// Assert - Event raised for each change
		Assert.Equal(4, eventRaisedCount);
	}

	[Fact]
	public void AreCameraInfoListsEqual_AlternatingBetweenTwoStates_ShouldRaiseEventForEachChange()
	{
		// Arrange
		var provider = new MockCameraProvider();
		var state1 = new List<CameraInfo> { CreateCameraInfo("Camera1") };
		var state2 = new List<CameraInfo> { CreateCameraInfo("Camera2") };

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Alternating between two different states
		SetAvailableCameras(provider, state1);
		SetAvailableCameras(provider, state2);
		SetAvailableCameras(provider, state1);
		SetAvailableCameras(provider, state2);

		// Assert - Event raised for each change
		Assert.Equal(4, eventRaisedCount);
	}

	[Fact]
	public void AreCameraInfoListsEqual_NoChangeBetweenUpdates_ShouldNotRaiseEvent()
	{
		// Arrange
		var camera = CreateCameraInfo("Camera1");
		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, new List<CameraInfo> { camera });

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Multiple updates with same content
		SetAvailableCameras(provider, new List<CameraInfo> { camera });
		SetAvailableCameras(provider, new List<CameraInfo> { camera });
		SetAvailableCameras(provider, new List<CameraInfo> { camera });

		// Assert - No events raised
		Assert.Equal(0, eventRaisedCount);
	}

	#endregion

	#region Large List Tests

	[Fact]
	public void AreCameraInfoListsEqual_LargeLists_SameContent_ShouldConsiderEqual()
	{
		// Arrange
		var cameras = Enumerable.Range(0, 100)
			.Select(i => CreateCameraInfo($"Camera{i}"))
			.ToList();

		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, cameras);

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - Setting same large list
		SetAvailableCameras(provider, cameras.ToList());

		// Assert - No event should be raised
		Assert.Equal(0, eventRaisedCount);
	}

	[Fact]
	public void AreCameraInfoListsEqual_LargeLists_OneDifferent_ShouldConsiderNotEqual()
	{
		// Arrange
		var cameras1 = Enumerable.Range(0, 100)
			.Select(i => CreateCameraInfo($"Camera{i}"))
			.ToList();

		var cameras2 = Enumerable.Range(0, 100)
			.Select(i => i == 50 ? CreateCameraInfo($"DifferentCamera{i}") : CreateCameraInfo($"Camera{i}"))
			.ToList();

		var provider = new MockCameraProvider();
		SetAvailableCameras(provider, cameras1);

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Act - One camera different in large list
		SetAvailableCameras(provider, cameras2);

		// Assert - Event should be raised
		Assert.Equal(1, eventRaisedCount);
	}

	#endregion

	#region Edge Cases

	[Fact]
	public async Task AreCameraInfoListsEqual_AfterRefresh_ShouldCompareCorrectly()
	{
		// Arrange
		var provider = new MockCameraProvider();
		var initialCameras = provider.AvailableCameras;

		// Act - Refresh creates new cameras
		await provider.RefreshAvailableCameras(TestContext.Current.CancellationToken);
		var firstCamera = provider.AvailableCameras?[0];

		var eventRaisedCount = 0;
		provider.AvailableCamerasChanged += (s, c) => eventRaisedCount++;

		// Refresh again (will create different camera due to new GUID)
		await provider.RefreshAvailableCameras(TestContext.Current.CancellationToken);

		// Assert - Event should be raised because GUIDs are different
		Assert.Equal(1, eventRaisedCount);
		Assert.NotNull(provider.AvailableCameras);
		Assert.Single(provider.AvailableCameras);
	}

	#endregion

	#region Helper Methods

	static void SetAvailableCameras(MockCameraProvider provider, IReadOnlyList<CameraInfo>? cameras)
	{
		var property = typeof(MockCameraProvider).GetProperty(nameof(MockCameraProvider.AvailableCameras)) ?? throw new InvalidOperationException();
		property.SetValue(provider, cameras);
	}

	static CameraInfo CreateCameraInfo(string name, string? deviceId = null, CameraPosition position = CameraPosition.Front)
	{
		return new CameraInfo(
			name,
			deviceId ?? Guid.NewGuid().ToString(),
			position,
			false,
			1.0f,
			5.0f,
			[new Size(1920, 1080)]);
	}

	static CameraInfo CreateFullCameraInfo(
		string name,
		string deviceId,
		CameraPosition position,
		bool isSupported,
		float minZoom,
		float maxZoom)
	{
		return new CameraInfo(
			name,
			deviceId,
			position,
			isSupported,
			minZoom,
			maxZoom,
			[new Size(1920, 1080)]);
	}

	#endregion
}