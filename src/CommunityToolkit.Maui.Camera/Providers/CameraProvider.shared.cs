﻿namespace CommunityToolkit.Maui.Core;

partial class CameraProvider : ICameraProvider
{
	public IReadOnlyList<CameraInfo>? AvailableCameras { get; private set; }

	public partial ValueTask RefreshAvailableCameras(CancellationToken token);
}