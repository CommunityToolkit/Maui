using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class CustomSizeAndPositionPopupPage : BasePage<CustomSizeAndPositionPopupViewModel>
{
	public CustomSizeAndPositionPopupPage(CustomSizeAndPositionPopupViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		//Required to ensure the FlowDirection Picker is populated with a default value
		// Without this, the FlowDirection Picker selection is uninitialized
		BindingContext.FlowDirectionSelectedIndex = 1;
	}
}