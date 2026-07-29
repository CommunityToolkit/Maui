using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class SharedBarcodeScanningPage : BasePage<SharedBarcodeScanningViewModel>
{
	public SharedBarcodeScanningPage(SharedBarcodeScanningViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
	
	protected override async void OnAppearing()
	{
		base.OnAppearing();

		var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
		await BindingContext.RefreshCamerasCommand.ExecuteAsync(cancellationTokenSource.Token);
	}
}