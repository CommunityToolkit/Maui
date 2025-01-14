using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class StatusBarBehaviorPage : BasePage<StatusBarBehaviorViewModel>
{
	const string tryStatusBarOnModalPageButtonText = "Try StatusBarBehavior on Modal Page";

	public StatusBarBehaviorPage(StatusBarBehaviorViewModel viewModel)
		: base(viewModel)
	{
		InitializeComponent();

		ModalPageButton.Text = tryStatusBarOnModalPageButtonText;
		ModalPageButton.Clicked += HandleModalButtonClicked;
	}

	protected override void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);

		var statusBarColor = Color.FromRgba(BindingContext.RedSliderValue, BindingContext.GreenSliderValue, BindingContext.BlueSliderValue, 1.0);
		Core.Platform.StatusBar.SetColor(statusBarColor);
	}

	static async void HandleModalButtonClicked(object? sender, EventArgs e)
	{
		var statusBarBehaviorModelPage = new StatusBarBehaviorPage(new StatusBarBehaviorViewModel());

		// Get Modal Page Button
		if (!TryGetModalPageButton((Microsoft.Maui.ILayout)((ScrollView)statusBarBehaviorModelPage.Content).Content, out var modalPageButton))
		{
			throw new InvalidOperationException("Unable To Find Modal Page Button");
		}

		// Change Text on Modal Page Button
		modalPageButton.Text = "Pop Modal Page";

		// Modify Behavior of Modal Page Button
		modalPageButton.Clicked -= HandleModalButtonClicked;
		modalPageButton.Clicked += async (_, _) => await Shell.Current.Navigation.PopModalAsync();

		await Shell.Current.Navigation.PushModalAsync(statusBarBehaviorModelPage);
	}

	static bool TryGetModalPageButton(Microsoft.Maui.ILayout layout, [NotNullWhen(true)] out Button? button)
	{
		button = null;

		foreach (var view in layout)
		{
			switch (view)
			{
				case Button { Text: tryStatusBarOnModalPageButtonText } modalPageButton:
					button = modalPageButton;
					return true;

				case Microsoft.Maui.ILayout nestedLayout:
					if (TryGetModalPageButton(nestedLayout, out var modalPageButtonInLayout))
					{
						button = modalPageButtonInLayout;
						return true;
					}
					break;
			}
		}

		return false;
	}
}