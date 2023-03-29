using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class LazyViewPage : BasePage<LazyViewViewModel>
{
	public LazyViewPage(LazyViewViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LazyActiviation.LoadViewAsync();
	}

	async void LoadLazyView_Clicked(object sender, EventArgs e)
	{
		await LazyUserAction.LoadViewAsync();
	}
}