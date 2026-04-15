namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class BrokenTitleViewShell : Shell
{
	public BrokenTitleViewShell()
	{
		InitializeComponent();

		GoToAsync("//Tabbar");
	}
}