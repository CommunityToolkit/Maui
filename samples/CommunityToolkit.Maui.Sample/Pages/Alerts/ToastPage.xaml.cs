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
		await toast.Show();
	}

	async void ShowCustomToastButtonClicked(object? sender, EventArgs args)
	{
		var toast = Toast.Make("This is a big Toast.", ToastDuration.Long, 30d);
		await toast.Show();
	}

	async void DisplayToastInModalButtonClicked(object? sender, EventArgs e)
	{
		if (Application.Current?.MainPage is not null)
		{
			await Application.Current.MainPage.Navigation.PushModalAsync(new ContentPage
			{
				Content = new VerticalStackLayout
				{
					Spacing = 12,

					Children =
					{
						new Button { Command = new AsyncRelayCommand(() => Toast.Make("Toast in a Modal Page").Show()) }
							.Top().CenterHorizontal()
							.Text("Display Toast"),

						new Label()
							.Center().TextCenter()
							.Text("This is a Modal Page"),

						new Button { Command = new AsyncRelayCommand(Application.Current.MainPage.Navigation.PopModalAsync) }
							.Bottom().CenterHorizontal()
							.Text("Back to Toast Page")
					}
				}.Center()
			}.Padding(12));
		}
	}
}