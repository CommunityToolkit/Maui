using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class PopupPositionPage : BasePage<PopupPositionViewModel>
{
	public PopupPositionPage(IDeviceInfo deviceInfo, PopupPositionViewModel popupPositionViewModel)
		: base(deviceInfo, popupPositionViewModel)
	{
		InitializeComponent();
	}
}