using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class MultipleButtonPopup : Popup
{
	public MultipleButtonPopup()
	{
		InitializeComponent();
	}

	void Cancel_Clicked(object? sender, System.EventArgs e) => Close(false);

	void Okay_Clicked(object? sender, System.EventArgs e) => Close(true);
}