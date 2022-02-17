using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class PopupAnchorPage : BasePage<PopupAnchorViewModel>
{
	public PopupAnchorPage(PopupAnchorViewModel popupAnchorViewModel)
		: base(popupAnchorViewModel)
	{
		InitializeComponent();
		Indicator ??= new();
	}

	void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var label = (Label)sender;

		if (Device.RuntimePlatform is Device.Android)
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