using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class MultipleButtonPopup
{
	public MultipleButtonPopup()
	{
		InitializeComponent();
	}

	void Cancel_Clicked(object? sender, System.EventArgs e) => Dismiss(false);

	void Okay_Clicked(object? sender, System.EventArgs e) => Dismiss(true);
}