using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class ExpanderPage : BasePage<ExpanderViewModel>
{
	public ExpanderPage(ExpanderViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
#if IOS || MACCATALYST || WINDOWS
		Expander.HandleExpandAction = Expander.DefaultHandleOnExpandAction;
#endif
	}

	async void Expander_ExpandedChanged(object sender, Core.ExpandedChangedEventArgs e)
	{
		var collapsedText = e.IsExpanded ? "expanded" : "collapsed";
		await Toast.Make($"Expander is {collapsedText}").Show(CancellationToken.None);
	}

	async void GoToCSharpSampleClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new ExpanderPageCS());
	}
}