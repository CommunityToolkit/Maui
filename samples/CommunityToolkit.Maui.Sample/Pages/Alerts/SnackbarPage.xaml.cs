using System.Reflection;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using Font = Microsoft.Maui.Font;

namespace CommunityToolkit.Maui.Sample.Pages.Alerts;

public partial class SnackbarPage : BasePage<SnackbarViewModel>
{
	const string displayCustomSnackbarText = "Display a Custom Snackbar, Anchored to this Button";
	const string dismissCustomSnackbarText = "Dismiss Custom Snackbar";
	readonly IReadOnlyList<Color> colors = typeof(Colors)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()))
		.Values.ToList();

	ISnackbar? customSnackbar;

	public SnackbarPage(IDeviceInfo deviceInfo, SnackbarViewModel snackbarViewModel) : base(deviceInfo, snackbarViewModel)
	{
		InitializeComponent();

		SnackbarShownStatus ??= new();
		DisplayCustomSnackbarButton ??= new();
		DisplayCustomSnackbarButton.Text = displayCustomSnackbarText;

		Snackbar.Shown += Snackbar_Shown;
		Snackbar.Dismissed += Snackbar_Dismissed;
	}

	async void DisplayDefaultSnackbarButtonClicked(object? sender, EventArgs args) =>
		await this.DisplaySnackbar("This is a Snackbar.\nIt will disappear in 3 seconds.\nOr click OK to dismiss immediately");

	async void DisplayCustomSnackbarButtonClicked(object? sender, EventArgs args)
	{
		if (DisplayCustomSnackbarButton.Text is displayCustomSnackbarText)
		{
			var options = new SnackbarOptions
			{
				BackgroundColor = Colors.Red,
				TextColor = Colors.Green,
				ActionButtonTextColor = Colors.Yellow,
				CornerRadius = new CornerRadius(10),
				Font = Font.SystemFontOfSize(14),
			};

			customSnackbar = Snackbar.Make(
				"This is a customized Snackbar",
				async () =>
				{
					await DisplayCustomSnackbarButton.BackgroundColorTo(colors[new Random().Next(colors.Count)], length: 500);
					DisplayCustomSnackbarButton.Text = displayCustomSnackbarText;
				},
				"Change Button Color",
				TimeSpan.FromSeconds(30),
				options,
				DisplayCustomSnackbarButton);

			await customSnackbar.Show();

			DisplayCustomSnackbarButton.Text = dismissCustomSnackbarText;
		}
		else if (DisplayCustomSnackbarButton.Text is dismissCustomSnackbarText)
		{
			if (customSnackbar is not null)
			{
				await customSnackbar.Dismiss();
			}

			DisplayCustomSnackbarButton.Text = displayCustomSnackbarText;
		}
		else
		{
			throw new NotSupportedException($"{nameof(DisplayCustomSnackbarButton)}.{nameof(ITextButton.Text)} Not Recognized");
		}
	}

	void Snackbar_Dismissed(object? sender, EventArgs e)
	{
		SnackbarShownStatus.Text = $"Snackbar dismissed. Snackbar.IsShown={Snackbar.IsShown}";
	}

	void Snackbar_Shown(object? sender, EventArgs e)
	{
		SnackbarShownStatus.Text = $"Snackbar shown. Snackbar.IsShown={Snackbar.IsShown}";
	}
}