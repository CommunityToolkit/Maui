using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.Pages.Views.Popup;

// TryShowPopupAsync and TryClosePopupAsync never throw for a popup failure: they return a PopupOutcome union that holds either the popup result or the exception
public partial class TryPopupMethodsPage : ContentPage
{
	const string popupResultText = "Hello from the Popup";

	readonly Label showOutcomeLabel, closeOutcomeLabel;

	bool isOpenInProgress;

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
					new Label().Text("TryShowPopupAsync and TryClosePopupAsync return a PopupOutcome: either the popup result or the exception, so a switch over the outcome handles every case without catching exceptions."),

					new Button { Command = new AsyncRelayCommand(ShowPopup) }.Text("TryShowPopupAsync"),
					new Button { Command = new AsyncRelayCommand(ShowPopupWithCanceledToken) }.Text("TryShowPopupAsync with a Canceled Token"),
					new Button { Command = new AsyncRelayCommand(ClosePopupWhenNoneIsShown) }.Text("TryClosePopupAsync without a Popup"),

					new Label().Text("TryShowPopupAsync Outcome").Font(bold: true),
					new Label().Text("None yet").Assign(out showOutcomeLabel),

					new Label().Text("TryClosePopupAsync Outcome").Font(bold: true),
					new Label().Text("None yet").Assign(out closeOutcomeLabel)
				}
			}
		};
	}

	static string Describe(PopupOutcome<string> outcome) => outcome switch
	{
		// Read Result only when the popup was not dismissed by tapping outside of it
		IPopupResult<string> { WasDismissedByTappingOutsideOfPopup: true } => "Dismissed by tapping outside of the Popup",
		IPopupResult<string> popupResult => $"Closed with result: {popupResult.Result ?? "null"}",
		OperationCanceledException => "Canceled: the Popup was not shown",
		Exception exception => $"{exception.GetType().Name}: {exception.Message}"
	};

	static string Describe(PopupOutcome outcome) => outcome switch
	{
		IPopupResult => "Popup closed",
		Exception exception => $"{exception.GetType().Name}: {exception.Message}"
	};

	async Task ShowPopup()
	{
		// A popup covers this page only once its push is done, so a quick second tap would open a second popup
		if (isOpenInProgress)
		{
			return;
		}

		isOpenInProgress = true;

		try
		{
			var popupContent = new VerticalStackLayout
			{
				Spacing = 12,
				Children =
				{
					new Label().Text("Close this Popup with a result, without one, or tap outside of it to dismiss it."),
					new Button { Command = new AsyncRelayCommand(async () => closeOutcomeLabel.Text = Describe(await this.TryClosePopupAsync(popupResultText))) }.Text("Close with a Result"),
					new Button { Command = new AsyncRelayCommand(async () => closeOutcomeLabel.Text = Describe(await this.TryClosePopupAsync())) }.Text("Close without a Result")
				}
			};

			showOutcomeLabel.Text = Describe(await this.TryShowPopupAsync<string>(popupContent));
		}
		finally
		{
			isOpenInProgress = false;
		}
	}

	async Task ShowPopupWithCanceledToken()
	{
		using var cancellationTokenSource = new CancellationTokenSource();
		await cancellationTokenSource.CancelAsync();

		showOutcomeLabel.Text = Describe(await this.TryShowPopupAsync<string>(new Label().Text("This Popup is never shown"), token: cancellationTokenSource.Token));
	}

	async Task ClosePopupWhenNoneIsShown()
	{
		closeOutcomeLabel.Text = Describe(await this.TryClosePopupAsync());
	}
}