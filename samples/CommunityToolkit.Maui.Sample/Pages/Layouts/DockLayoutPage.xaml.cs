using CommunityToolkit.Maui.Sample.ViewModels.Layouts;

namespace CommunityToolkit.Maui.Sample.Pages.Layouts;

public partial class DockLayoutPage : BasePage<DockLayoutViewModel>
{
	public DockLayoutPage(DockLayoutViewModel dockLayoutViewModel)
		: base(dockLayoutViewModel)
	{
		InitializeComponent();
	}
}