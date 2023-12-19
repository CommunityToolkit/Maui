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

		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		await LazyActiviation.LoadViewAsync(cts.Token);
	}

	async void LoadLazyView_Clicked(object sender, EventArgs e)
	{
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		await LazyUserAction.LoadViewAsync(cts.Token);
	}
}