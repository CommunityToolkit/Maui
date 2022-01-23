using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class NoLightDismissPopup
{
	public NoLightDismissPopup()
	{
		InitializeComponent();
	}

	void Button_Clicked(object? sender, System.EventArgs e) => Dismiss(null);
}