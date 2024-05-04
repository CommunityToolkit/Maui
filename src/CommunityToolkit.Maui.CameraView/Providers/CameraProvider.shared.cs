namespace CommunityToolkit.Maui.Core;

public partial class CameraProvider
{
    public IReadOnlyList<CameraInfo> AvailableCameras { get; private set; } = [];
	
	public CameraProvider()
	{
		RefreshAvailableCameras();

		async void RefreshAvailableCameras() => await this.RefreshAvailableCameras(CancellationToken.None);
	}

    public partial ValueTask RefreshAvailableCameras(CancellationToken token);
	
}
