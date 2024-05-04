using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Core;

public partial class CameraProvider
{
    public IReadOnlyList<CameraInfo> AvailableCameras { get; private set; } = [];
	
	public CameraProvider()
	{
		RefreshAvailableCameras();
	}

    public partial void RefreshAvailableCameras();
	
}
