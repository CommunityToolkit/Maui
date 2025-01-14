using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class MultipleButtonPopup : Maui.Views.Popup
{
	public MultipleButtonPopup()
	{
		InitializeComponent();
	}

	void Cancel_Clicked(object? sender, EventArgs e)
	{
		Close(false);
	}

	void Okay_Clicked(object? sender, EventArgs e)
	{
		Close(true);
	}
}