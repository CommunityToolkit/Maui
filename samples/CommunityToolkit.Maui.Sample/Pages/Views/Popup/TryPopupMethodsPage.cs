using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.Pages.Views.Popup;

public partial class TryPopupMethodsPage : ContentPage
{
	const string popupResultText = "Hello from the Popup";

	readonly Label showResultOrExceptionLabel, closeResultOrExceptionLabel;

	public TryPopupMethodsPage()
	{
		Title = "Try Popup Methods";
		Padding = 12;

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Spacing = 12,
				Children =
				{
					new Label().Text("TryShowPopupAsync and TryClosePopupAsync return a PopupResultOrException that holds either the IPopupResult of the closed popup or the Exception that the operation failed with, so one switch handles every case without catching exceptions."),

					new Button { Command = new AsyncRelayCommand(ShowPopup) }.Text("TryShowPopupAsync"),
					new Button { Command = new AsyncRelayCommand(ShowPopupWithoutResultType) }.Text("TryShowPopupAsync without a Result Type"),
					new Button { Command = new AsyncRelayCommand(ShowPopupWithCanceledToken) }.Text("TryShowPopupAsync with a Canceled Token"),
					new Button { Command = new AsyncRelayCommand(ClosePopupWhenNoneIsShown) }.Text("TryClosePopupAsync without a Popup"),

					new Label().Text("TryShowPopupAsync Result or Exception").Font(bold: true),
					new Label().Text("None yet").Assign(out showResultOrExceptionLabel),

					new Label().Text("TryClosePopupAsync Result or Exception").Font(bold: true),
					new Label().Text("None yet").Assign(out closeResultOrExceptionLabel)
				}
			}
		};
	}

	static string Describe(PopupResultOrException resultOrException) => resultOrException switch
	{
		IPopupResult { WasDismissedByTappingOutsideOfPopup: true } => "Dismissed: tapped outside, back button or navigation",
		IPopupResult => "Popup closed",
		OperationCanceledException => "Canceled: the Popup was not shown",
		Exception exception => $"{exception.GetType().Name}: {exception.Message}"
	};

	async Task ShowPopup()
	{
		var popupContent = CreatePopupContent();
		showResultOrExceptionLabel.Text = Describe(await this.TryShowPopupAsync<string>(popupContent));
	}

	async Task ShowPopupWithoutResultType()
	{
		var popupContent = CreatePopupContent();
		showResultOrExceptionLabel.Text = Describe(await this.TryShowPopupAsync(popupContent));
	}

	VerticalStackLayout CreatePopupContent() => new()
	{
		Spacing = 12,
		Children =
		{
			new Label().Text("Close this Popup with a result, without one, or tap outside of it to dismiss it."),
			new Button { Command = new AsyncRelayCommand(async () => closeResultOrExceptionLabel.Text = Describe(await this.TryClosePopupAsync(popupResultText))) }.Text("Close with a Result"),
			new Button { Command = new AsyncRelayCommand(async () => closeResultOrExceptionLabel.Text = Describe(await this.TryClosePopupAsync())) }.Text("Close without a Result"),
			new Button { Command = new AsyncRelayCommand(ShowPageAbovePopup) }.Text("Show a Page Above the Popup")
		}
	};

	async Task ShowPageAbovePopup()
	{
		var pageAbovePopup = new ContentPage
		{
			Title = "Page Above the Popup",
			Padding = 12
		};

		pageAbovePopup.Content = new VerticalStackLayout
		{
			Spacing = 12,
			Children =
			{
				new Button { Command = new AsyncRelayCommand(async () => closeResultOrExceptionLabel.Text = Describe(await this.TryClosePopupAsync())) }.Text("TryClosePopupAsync (Blocked)"),
				new Button { Command = new AsyncRelayCommand(() => pageAbovePopup.Navigation.PopModalAsync()) }.Text("Back to the Popup")
			}
		};

		await Navigation.PushModalAsync(pageAbovePopup);
	}

	async Task ShowPopupWithCanceledToken()
	{
		using var cancellationTokenSource = new CancellationTokenSource();
		await cancellationTokenSource.CancelAsync();

		showResultOrExceptionLabel.Text = Describe(await this.TryShowPopupAsync<string>(new Label().Text("This Popup is never shown"), token: cancellationTokenSource.Token));
	}

	async Task ClosePopupWhenNoneIsShown()
	{
		closeResultOrExceptionLabel.Text = Describe(await this.TryClosePopupAsync());
	}
}