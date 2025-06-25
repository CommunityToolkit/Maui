using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.Pages.Views.Popup;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Sample.Views.Popups;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class PopupsPage : BasePage<PopupsViewModel>
{
	readonly IPopupService popupService;

	public PopupsPage(PopupsViewModel multiplePopupViewModel, IPopupService popupService)
		: base(multiplePopupViewModel)
	{
		this.popupService = popupService;

		InitializeComponent();
	}

	async void HandleSimplePopupButtonClicked(object sender, EventArgs e)
	{
		var queryAttributes = new Dictionary<string, object>
		{
			["DescriptionLabel"] = "This is a popup where this text is being passed in using IQueryAttributable"
		};

		await popupService.ShowPopupAsync<SimplePopup>(Shell.Current, new PopupOptions
		{
			Shape = new RoundRectangle
			{
				CornerRadius = new CornerRadius(4),
				Stroke = Colors.White
			}
		}, queryAttributes, CancellationToken.None);
	}

	async void HandleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<ButtonPopup>(Navigation);
	}

	async void HandleMultipleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<MultipleButtonPopup, bool>(Navigation);
	}

	async void HandleNoOutsideTapDismissPopupClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<NoOutsideTapDismissPopup>(Navigation, new PopupOptions
		{
			CanBeDismissedByTappingOutsideOfPopup = false,
		});
	}

	async void HandleToggleSizePopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<ToggleSizePopup>(Navigation);
	}

	async void HandleTransparentPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<TransparentPopup>(Navigation);
	}

	async void HandleOpenedEventSimplePopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<OpenedEventSimplePopup>(Navigation, new PopupOptions
		{
			Shape = new RoundRectangle
			{
				CornerRadius = new CornerRadius(20, 20, 20, 20),
				StrokeThickness = 20,
				Stroke = Colors.Blue
			},
		});
	}

	async void HandleReturnResultPopupButtonClicked(object sender, EventArgs e)
	{
		var result = await popupService.ShowPopupAsync<ReturnResultPopup, string>(Navigation);

		await DisplayAlert("Pop Result Returned", $"Result: {result.Result ?? "Closed by tapping outside"}", "OK");
	}

	async void HandleXamlBindingPopupPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<XamlBindingPopup>(Navigation);
	}

	async void HandlePopupPositionButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new PopupPositionPage(new PopupPositionViewModel()));
	}

	async void HandlePopupLayoutAlignmentButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new PopupLayoutAlignmentPage(new PopupLayoutAlignmentViewModel()));
	}

	async void HandlePopupSizingIssuesButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new PopupSizingIssuesPage(new PopupSizingIssuesViewModel()));
	}

	async void HandleShowPopupInOnAppearingButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new ShowPopupInOnAppearingPage(new ShowPopupInOnAppearingPageViewModel(), popupService));
	}

	async void HandleStylePopupButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new StylePopupPage(new StylePopupViewModel()));
	}

	async void HandleOnDisappearingPopupClicked(object sender, EventArgs e)
	{
		await Navigation.PushModalAsync(new PopupOnDisappearingPage());
	}

	async void HandleSelfClosingPopupButtonClicked(object? sender, EventArgs e)
	{
		this.ShowPopup(new Label().Text("This Popup Will Close Automatically in 2 Seconds"), new PopupOptions
		{
			CanBeDismissedByTappingOutsideOfPopup = false
		});

		await Task.Delay(TimeSpan.FromSeconds(2));

		await this.ClosePopupAsync();
	}

	async void HandleComplexPopupClicked(object? sender, EventArgs e)
	{
		var complexPopupOptionsViewModel = new ComplexPopupOptionsViewModel();
		var complexPopupOptions = new PopupOptions
		{
			BindingContext = complexPopupOptionsViewModel,
			Shape = new RoundRectangle
			{
				CornerRadius = new CornerRadius(4),
				StrokeThickness = 12,
				Stroke = Colors.Orange
			}
		};
		complexPopupOptions.SetBinding<ComplexPopupOptionsViewModel, Color>(PopupOptions.PageOverlayColorProperty, static x => x.PageOverlayBackgroundColor, source: complexPopupOptionsViewModel);

		var popupResultTask = popupService.ShowPopupAsync<ComplexPopup, string>(Navigation, complexPopupOptions);

		// Rotate `PopupOptions.PageOverlayBackgroundColor` every 2 seconds using random colors 
		while (!popupResultTask.IsCompleted)
		{
			await Task.Delay(TimeSpan.FromSeconds(2));
			
			complexPopupOptionsViewModel.PageOverlayBackgroundColor =
				Color.FromRgba(Random.Shared.NextDouble(), Random.Shared.NextDouble(), Random.Shared.NextDouble(), 0.2f);
		}

		var popupResult = await popupResultTask;
		if (!popupResult.WasDismissedByTappingOutsideOfPopup)
		{
			// Display Popup Result as a Toast
			await Toast.Make($"You entered {popupResult.Result}").Show();
		}
	}
}