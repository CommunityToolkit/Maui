using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Maui.Alerts.Snackbar;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Sample.Pages.Alerts;

public partial class SnackbarPage : BasePage
{
	const string _displayCustomSnackbarText = "Display a Custom Snackbar, Anchored to this Button";
	const string _dismissCustomSnackbarText = "Dismiss Custom Snackbar";
	readonly IReadOnlyList<Color> _colors = typeof(Colors)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()))
		.Values.ToList();

	ISnackbar? _customSnackbar;

	public SnackbarPage()
	{
		InitializeComponent();

		DisplayCustomSnackbarButton ??= new();
		DisplayCustomSnackbarButton.Text = _displayCustomSnackbarText;
	}

	async void DisplayDefaultSnackbarButtonClicked(object? sender, EventArgs args) =>
		await this.DisplaySnackbar("This is a Snackbar.\nIt will disappear in 3 seconds.\nOr click OK to dismiss immediately");

	async void DisplayCustomSnackbarButtonClicked(object? sender, EventArgs args)
	{
		if (DisplayCustomSnackbarButton.Text is _displayCustomSnackbarText)
		{
			var options = new SnackbarOptions
			{
				BackgroundColor = Colors.Red,
				TextColor = Colors.Green,
				ActionButtonTextColor = Colors.Yellow,
				CornerRadius = new CornerRadius(10),
				Font = Font.SystemFontOfSize(14),
			};

			_customSnackbar = Snackbar.Make(
				"This is a customized Snackbar",
				async () =>
				{
					await DisplayCustomSnackbarButton.BackgroundColorTo(_colors[new Random().Next(_colors.Count)], length: 500);
					DisplayCustomSnackbarButton.Text = _displayCustomSnackbarText;
				},
				"Change Button Color",
				TimeSpan.FromSeconds(30),
				options,
				DisplayCustomSnackbarButton);

			await _customSnackbar.Show();

			DisplayCustomSnackbarButton.Text = _dismissCustomSnackbarText;
		}
		else if (DisplayCustomSnackbarButton.Text is _dismissCustomSnackbarText)
		{
			if (_customSnackbar is not null)
				await _customSnackbar.Dismiss();

			DisplayCustomSnackbarButton.Text = _displayCustomSnackbarText;
		}
		else
		{
			throw new NotImplementedException($"{nameof(DisplayCustomSnackbarButton)}.{nameof(ITextButton.Text)} Not Recognized");
		}
	}
}