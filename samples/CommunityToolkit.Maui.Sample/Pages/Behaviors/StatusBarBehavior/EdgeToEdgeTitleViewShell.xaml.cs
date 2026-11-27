using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class EdgeToEdgeTitleViewShell : Shell
{
	public EdgeToEdgeTitleViewShell()
	{
		InitializeComponent();

		BindingContext = new EdgeToEdgeShellViewModel();
	}
}