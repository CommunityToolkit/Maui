using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class PopupAnchorPage : BasePage<PopupAnchorViewModel>
{
	readonly IDeviceInfo deviceinfo;

	public PopupAnchorPage(IDeviceInfo deviceInfo, PopupAnchorViewModel popupAnchorViewModel)
		: base(deviceInfo, popupAnchorViewModel)
	{
		InitializeComponent();
		Indicator ??= new();

		deviceinfo = deviceInfo;
	}

	void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var label = (Label)sender;

		if (deviceinfo.Platform == DevicePlatform.Android)
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