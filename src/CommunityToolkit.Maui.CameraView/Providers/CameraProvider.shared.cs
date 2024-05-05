namespace CommunityToolkit.Maui.Core;

public partial class CameraProvider
{
    public IReadOnlyList<CameraInfo> AvailableCameras { get; private set; } = [];
	
	public CameraProvider()
	{
		RefreshAvailableCameras();

		// async void allows us to fire-and-forget the ValueTask while still awaiting it 
		async void RefreshAvailableCameras() => await this.RefreshAvailableCameras(CancellationToken.None);
	}

    public partial ValueTask RefreshAvailableCameras(CancellationToken token);
	
}
