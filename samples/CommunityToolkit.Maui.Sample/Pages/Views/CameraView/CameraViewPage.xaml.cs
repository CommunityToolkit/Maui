using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class CameraViewPage : BasePage<CameraViewModel>
{
	public CameraViewPage(CameraViewModel cameraViewModel) : base(cameraViewModel)
	{
		InitializeComponent();

		this.camera.MediaCaptured += (sender, args) =>
		{
			Dispatcher.Dispatch(() =>
			{
				image.Source = ImageSource.FromStream(() => args.Media);
			});
		};
	}

	void Shutter(object sender, EventArgs e)
	{
		camera.Shutter();
	}
}