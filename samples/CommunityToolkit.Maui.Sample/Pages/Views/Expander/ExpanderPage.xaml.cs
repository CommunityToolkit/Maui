using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class ExpanderPage : BasePage<ExpanderViewModel>
{
	public ExpanderPage(ExpanderViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}

	Dictionary<Expander, long> expandTimes = new();

	void Expander_ExpandedChanging(object? sender, Core.ExpandedChangingEventArgs e)
	{
		if (sender is Expander expander)
		{
			expandTimes[expander] = DateTime.Now.Ticks;
		}
	}

	async void Expander_ExpandedChanged(object? sender, Core.ExpandedChangedEventArgs e)
	{
		var collapsedText = e.IsExpanded ? "expanded" : "collapsed";
		if (sender is Expander expander)
		{
			if (expandTimes.TryGetValue(expander, out var startTime))
			{
				var elapsed = DateTime.Now.Ticks - startTime;
				await Toast.Make($"Expander is {collapsedText} ({elapsed} ms)").Show(CancellationToken.None);
			}
			else
			{
				await Toast.Make($"Expander is {collapsedText}").Show(CancellationToken.None);
			}
		}
	}

	async void GoToCSharpSampleClicked(object? sender, EventArgs? e)
	{
		await Navigation.PushAsync(new ExpanderPageCS());
	}
}