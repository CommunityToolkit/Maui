using System.Reflection;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.Resources.Fonts;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Mvvm.Input;
using Font = Microsoft.Maui.Font;

namespace CommunityToolkit.Maui.Sample.Pages.Alerts;

public partial class SnackbarPage : BasePage<SnackbarViewModel>
{
	public const string DisplayCustomSnackbarText = "Display Custom Snackbar";
	const string dismissCustomSnackbarText = "Dismiss Custom Snackbar";

	readonly IReadOnlyList<Color> colors = [.. typeof(Colors)
											.GetFields(BindingFlags.Static | BindingFlags.Public)
											.ToDictionary(c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()))
											.Values];

	ISnackbar? customSnackbar;

	public SnackbarPage(SnackbarViewModel snackbarViewModel) : base(snackbarViewModel)
	{
		InitializeComponent();

		DisplayCustomSnackbarButtonAnchoredToButton.Text = DisplayCustomSnackbarText;

		Snackbar.Shown += Snackbar_Shown;
		Snackbar.Dismissed += Snackbar_Dismissed;
	}

	async void DisplayDefaultSnackbarButtonClicked(object? sender, EventArgs args) =>
		await this.DisplaySnackbar("This is a Snackbar.\nIt will disappear in 3 seconds.\nOr click OK to dismiss immediately");

	async void DisplayCustomSnackbarAnchoredToButtonClicked(object? sender, EventArgs args)
	{
		if (DisplayCustomSnackbarButtonAnchoredToButton.Text is DisplayCustomSnackbarText)
		{
			var options = new SnackbarOptions
			{
				BackgroundColor = Colors.Red,
				TextColor = Colors.Green,
				ActionButtonTextColor = Colors.Yellow,
				CornerRadius = new CornerRadius(10),
				Font = Font.SystemFontOfSize(14),
				ActionButtonFont = Font.OfSize(FontFamilies.FontAwesomeBrands, 16, enableScaling: false),
			};

			customSnackbar = Snackbar.Make(
				"This is a customized Snackbar",
				async () =>
				{
					await DisplayCustomSnackbarButtonAnchoredToButton.BackgroundColorTo(colors[Random.Shared.Next(colors.Count)], length: 500);
					DisplayCustomSnackbarButtonAnchoredToButton.Text = DisplayCustomSnackbarText;
				},
				FontAwesomeIcons.Microsoft,
				TimeSpan.FromSeconds(30),
				options,
				DisplayCustomSnackbarButtonAnchoredToButton);

			var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			await customSnackbar.Show(cts.Token);

			DisplayCustomSnackbarButtonAnchoredToButton.Text = dismissCustomSnackbarText;
		}
		else if (DisplayCustomSnackbarButtonAnchoredToButton.Text is dismissCustomSnackbarText)
		{
			if (customSnackbar is not null)
			{
				var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
				await customSnackbar.Dismiss(cts.Token);

				customSnackbar.Dispose();
			}

			DisplayCustomSnackbarButtonAnchoredToButton.Text = DisplayCustomSnackbarText;
		}
		else
		{
			throw new NotSupportedException($"{nameof(DisplayCustomSnackbarButtonAnchoredToButton)}.{nameof(ITextButton.Text)} Not Recognized");
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

	async void DisplaySnackbarInModalButtonClicked(object? sender, EventArgs e)
	{
		if (Application.Current?.Windows[0].Page is Page mainPage)
		{
			var button = new Button()
				.CenterHorizontal()
				.Text("Display Snackbar");
			button.Command = new AsyncRelayCommand(token => button.DisplaySnackbar(
				"This Snackbar is anchored to the Display Snackbar button at the bottom of the Page to avoid clipping the Snackbar on the top of the Page.",
				() => { },
				"Close",
				TimeSpan.FromSeconds(5), token: token));

			var backButton = new Button()
				.CenterHorizontal()
				.Text("Back to Snackbar MainPage");
			backButton.Command = new AsyncRelayCommand(mainPage.Navigation.PopModalAsync);

			await mainPage.Navigation.PushModalAsync(new ContentPage
			{
				// A Snackbar anchored to a View appears above that View (on iOS and MacCatalyst, below it when there is no room above it),
				// so the buttons are at the bottom of the Page, and the Back button is below the anchor, where the Snackbar does not cover it
				Content = new VerticalStackLayout
				{
					Spacing = 12,

					Children =
					{
						button,

						backButton
					}
				}.Bottom()
			}.Padding(12));
		}
	}
}