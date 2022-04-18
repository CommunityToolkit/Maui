namespace CommunityToolkit.Maui.Sample;

public partial class CameraTestPage : ContentPage
{
	public CameraTestPage()
	{
		InitializeComponent();
	}

	void Button_Clicked(object sender, EventArgs e)
	{
		camera.Shutter();
	}
}