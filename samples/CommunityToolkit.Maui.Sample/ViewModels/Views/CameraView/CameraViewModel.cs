using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public class CameraViewModel : BaseViewModel
{
	public ICollection<CameraFlashMode> FlashModes => Enum.GetValues<CameraFlashMode>();

	CameraFlashMode flashMode;

	public CameraFlashMode FlashMode
	{
		get => flashMode;
		set => SetProperty(ref flashMode, value);
	}
}