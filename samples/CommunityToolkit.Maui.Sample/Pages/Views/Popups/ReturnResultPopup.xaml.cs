using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class ReturnResultPopup
{
	public ReturnResultPopup()
	{
		InitializeComponent();
	}


	protected override object GetLightDismissResult() => "Light Dismiss";

	void Button_Clicked(object? sender, System.EventArgs e) => Dismiss("Close button tapped");
}