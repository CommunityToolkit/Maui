namespace CommunityToolkit.Maui.Sample.Pages;

public partial class MainPage : BasePage
{
	public MainPage()
	{
		InitializeComponent();

		Page ??= this;

		Padding = new Thickness(20, 0);
	}
}