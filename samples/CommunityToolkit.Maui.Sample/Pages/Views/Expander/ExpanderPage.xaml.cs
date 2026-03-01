using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class ExpanderPage : BasePage<ExpanderViewModel>
{
	public ExpanderPage(ExpanderViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}

	Stopwatch stopWatch = new();

	void Expander_ExpandedChanging(object? sender, Core.ExpandedChangingEventArgs e)
	{
		stopWatch.Restart();
	}

	async void Expander_ExpandedChanged(object? sender, Core.ExpandedChangedEventArgs e)
	{
		stopWatch.Stop();
		var collapsedText = e.IsExpanded ? "expanded" : "collapsed";
		await Toast.Make($"Expander is {collapsedText} ({stopWatch.ElapsedMilliseconds} ms)").Show(CancellationToken.None);
	}

	async void GoToCSharpSampleClicked(object? sender, EventArgs? e)
	{
		await Navigation.PushAsync(new ExpanderPageCS());
	}
}