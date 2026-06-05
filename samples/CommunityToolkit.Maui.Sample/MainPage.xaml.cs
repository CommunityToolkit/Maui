using System.Diagnostics;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.Views.Popups;

namespace Popup_Repro;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
	}

	async void OnCounterClicked(object? sender, EventArgs e)
	{
		count++;

		if (count == 1)
		{
			CounterBtn.Text = $"Clicked {count} time";
		}
		else
		{
			CounterBtn.Text = $"Clicked {count} times";
		}

		SemanticScreenReader.Announce(CounterBtn.Text);

		var buttonPopup = new SimplePopup();
		await this.ShowPopupAsync(buttonPopup);

		Entry.Unfocus();
	}

	async void Entry_Unfocused(System.Object? sender, Microsoft.Maui.Controls.FocusEventArgs e)
	{
		var unfocusedPopup = new ButtonPopup();

		await this.ShowPopupAsync(unfocusedPopup);
	}
}
