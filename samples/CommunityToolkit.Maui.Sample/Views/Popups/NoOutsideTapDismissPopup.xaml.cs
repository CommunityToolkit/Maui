using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class NoOutsideTapDismissPopup : Popup
{
	public NoOutsideTapDismissPopup()
	{
		InitializeComponent();
	}

	void Button_Clicked(object? sender, System.EventArgs e) => Close();
}