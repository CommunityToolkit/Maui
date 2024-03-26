using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Core;

public partial class CameraProvider
{
    public ObservableCollection<CameraInfo> AvailableCameras { get; internal set; } = new();

    public partial void RefreshAvailableCameras();

    public CameraProvider()
    {
        RefreshAvailableCameras();
    }
}

#if !ANDROID && !IOS && !WINDOWS
public partial class CameraProvider
{
    public partial void RefreshAvailableCameras()
    {
    }
}
#endif
