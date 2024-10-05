using System.Globalization;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.Essentials;

namespace CommunityToolkit.Maui.Sample.Pages.Essentials;

public partial class OfflineSpeechToTextPage : BasePage<OfflineSpeechToTextViewModel>
{
	public OfflineSpeechToTextPage(OfflineSpeechToTextViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		await BindingContext.SetLocalesCommand.ExecuteAsync(null);
	}
}