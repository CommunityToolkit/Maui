namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class OverflowingTitleViewPage : ContentPage
{
	public OverflowingTitleViewPage()
	{
		InitializeComponent();
	}

	void OnBackButtonClicked(object? sender, EventArgs e)
	{
		if (Window?.Page is BrokenTitleViewShell brokenTitleViewShell)
		{
			brokenTitleViewShell.ReturnToPreviousPage();
		}
	}
}