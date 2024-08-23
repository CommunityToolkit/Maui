// Ignore Spelling: csharp
namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class RatingViewXamlViewModel : BaseViewModel
{
	[ObservableProperty]
	double stepperValueMaximumRatings = 1;
}