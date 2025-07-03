using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class ComplexPopup : Popup<string>
{
	public ComplexPopup(ComplexPopupViewModel viewModel)
	{
		InitializeComponent();

		CanBeDismissedByTappingOutsideOfPopup = false;

		BindingContext = viewModel;
		Opened += HandlePopupOpened;
	}

	async void HandlePopupOpened(object? sender, EventArgs e)
	{
		// Delay for one second to ensure the user sees the previous text
		await Task.Delay(TimeSpan.FromSeconds(1));
		DescriptionLabel.Text = "This Popup demonstrates constructor injection to pass in a value using Dependency Injection using PopupService, demonstrates how to use the Opened event to trigger an action once the Popup appears, demonstrates how to bind to PopupOptions, and demonstrates how to return a result.";
	}
}