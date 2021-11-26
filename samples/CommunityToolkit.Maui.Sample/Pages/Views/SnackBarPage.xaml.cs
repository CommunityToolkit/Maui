using System;
using System.Text;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views.Popup.Snackbar;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class SnackbarPage : BasePage
{
	ISnackbar? snackbarWithAnchor;
	public SnackbarPage()
	{
		InitializeComponent();

		Anchor1 ??= new();
		StatusText ??= new();
	}

	async void DisplaySnackbarClicked(object? sender, EventArgs args)
	{
		await this.DisplaySnackbar(GenerateLongText(5), () =>
		{
			StatusText.Text = "Snackbar action button clicked";
		});
	}

	async void DisplaySnackbarAnchoredClicked(object? sender, EventArgs args)
	{
		var options = new SnackbarOptions
		{
			BackgroundColor = Colors.Red,
			TextColor = Colors.Green,
			ActionButtonTextColor = Colors.Yellow,
			CornerRadius = new CornerRadius(10, 20, 30, 40),
			Font = Font.SystemFontOfSize(20),
			CharacterSpacing = 1
		};

		snackbarWithAnchor = Snackbar.Make(
			"Customized snackbar",
			() => StatusText.Text = "Snackbar action button clicked",
			"Run action",
			TimeSpan.FromSeconds(30),
			options,
			Anchor1);

		await snackbarWithAnchor.Show();
	}

	async void DismissSnackbarClicked(object sender, System.EventArgs e)
	{
		if (snackbarWithAnchor is not null)
			await snackbarWithAnchor.Dismiss();
	}

	static string GenerateLongText(int stringDuplicationTimes)
	{
		const string message = "It is a very long message to test multiple strings. A B C D E F G H I I J K L M N O P Q R S T U V W X Y Z";
		var result = new StringBuilder();
		for (var i = 0; i < stringDuplicationTimes; i++)
		{
			result.AppendLine(message);
		}

		return result.ToString();
	}
}