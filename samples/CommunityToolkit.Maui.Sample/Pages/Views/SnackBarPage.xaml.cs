using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Controls.Snackbar;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class SnackBarPage : BasePage
{
	private Snackbar? snackbarWithAnchor;
	public SnackBarPage()
	{
		InitializeComponent();
		this.Anchor1 ??= new ();
		this.StatusText ??= new ();
	}

	async void DisplaySnackBarClicked(object? sender, EventArgs args)
	{
		await this.DisplaySnackBarAsync(GenerateLongText(5), "Run action", () =>
		{
			StatusText.Text = "SnackBar action button clicked";
		});
	}

	async void DisplaySnackbarAnchoredClicked(object? sender, EventArgs args)
	{
		snackbarWithAnchor = Snackbar.Make(
			GenerateLongText(5),
			TimeSpan.FromSeconds(30),
			() => StatusText.Text = "SnackBar action button clicked",
			Anchor1);
		var options = new SnackbarOptions();
		options.BackgroundColor = Colors.Red;
		options.TextColor = Colors.Green;
		options.ActionTextColor = Colors.Yellow;
		options.CornerRadius = new CornerRadius(10, 20, 30, 40);
		options.Font = Font.SystemFontOfSize(20);
		options.CharacterSpacing = 1;
		snackbarWithAnchor.VisualOptions = options;
		await snackbarWithAnchor.Show();
	}

	async void DismissSnackbarClicked(System.Object sender, System.EventArgs e)
	{
		if (snackbarWithAnchor is not null)
			await snackbarWithAnchor.Dismiss();
	}

	string GenerateLongText(int stringDuplicationTimes)
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
