using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

[RequiresUnreferencedCode("Expander is not trim safe")]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public partial class ExpanderPage : BasePage<ExpanderViewModel>
{
	public ExpanderPage(ExpanderViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
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

	async void AnimateExpander(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == CommunityToolkit.Maui.Views.Expander.IsExpandedProperty.PropertyName)
		{
			CommunityToolkit.Maui.Views.Expander expander = (CommunityToolkit.Maui.Views.Expander)sender;
			expander.ContentHeight = expander.IsExpanded ? expander.MinimumContentHeight : expander.MaximumContentHeight;
			await expander.ContentHeightTo(expander.IsExpanded ? expander.MaximumContentHeight : expander.MinimumContentHeight, 250, Easing.CubicInOut);
		}
	}
}
