using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class PopupAnchorPage : BasePage<PopupAnchorViewModel>
{
	readonly IDeviceInfo deviceInfo;

	public PopupAnchorPage(IDeviceInfo deviceInfo, PopupAnchorViewModel popupAnchorViewModel)
		: base(popupAnchorViewModel)
	{
		InitializeComponent();

		this.deviceInfo = deviceInfo;
	}

	void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var label = (Label)sender;

		if (deviceInfo.Platform == DevicePlatform.Android)
		{
			label.TranslationX += e.TotalX;
			label.TranslationY += e.TotalY;
		}
		else
		{
			switch (e.StatusType)
			{
				case GestureStatus.Running:
					label.TranslationX = e.TotalX;
					label.TranslationY = e.TotalY;
					break;
				case GestureStatus.Completed:
					label.TranslationX += e.TotalX;
					label.TranslationY += e.TotalY;
					break;
			}
		}
	}
}