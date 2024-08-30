using System.Reflection;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class RatingViewInspirationsPage : BasePage<RatingViewInspirationsViewModel>
{
	public RatingViewInspirationsPage(RatingViewInspirationsViewModel viewModel) : base(viewModel) => InitializeComponent();
}