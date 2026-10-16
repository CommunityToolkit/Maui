using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.Pages.Alerts;

public partial class ToastPage : BasePage<ToastViewModel>
{
	public ToastPage(ToastViewModel toastViewModel) : base(toastViewModel)
	{
		InitializeComponent();
	}

	async void ShowToastButtonClicked(object? sender, EventArgs args)
	{
		var toast = Toast.Make("This is a default Toast.");

		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		await toast.Show(cts.Token);
	}

	async void ShowCustomToastButtonClicked(object? sender, EventArgs args)
	{
		var toast = Toast.Make("This is a big Toast.", ToastDuration.Long, 30d);

		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		await toast.Show(cts.Token);
	}

	async void DisplayToastInModalButtonClicked(object? sender, EventArgs e)
	{
		if (Application.Current?.Windows[0].Page is Page mainPage)
		{
			var modalPage = new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Spacing = 12,

					Children =
					{
						new Button { Command = new AsyncRelayCommand(token => Toast.Make("Toast in a Modal MainPage").Show(token)) }
							.Top().CenterHorizontal()
							.Text("Display Toast"),

						new Label()
							.Center().TextCenter()
							.Text("This is a Modal MainPage"),

						new Button { Command = new AsyncRelayCommand(mainPage.Navigation.PopModalAsync) }
							.Bottom().CenterHorizontal()
							.Text("Back to Toast MainPage")
					}
				}.Center()
			}.Padding(12);

			// The implicit Page style makes every Page Transparent on Windows, so that the Mica backdrop of the window shows through it. On Windows, .NET MAUI keeps
			// the Page below a modal Page that has a BackgroundColor on screen, where it shows through the Transparent modal Page: without one, the modal Page replaces it
			if (OperatingSystem.IsWindows())
			{
				modalPage.SetValue(BackgroundColorProperty, null);
			}

			await mainPage.Navigation.PushModalAsync(modalPage);
		}
	}
}