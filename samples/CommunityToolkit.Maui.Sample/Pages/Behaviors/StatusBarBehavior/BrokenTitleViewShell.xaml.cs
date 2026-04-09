namespace CommunityToolkit.Maui.Sample.Pages.Behaviors.StatusBarBehavior;

public partial class BrokenTitleViewShell : Shell
{
	public BrokenTitleViewShell()
	{
		InitializeComponent();

		GoToAsync("//Tabbar");
	}
}