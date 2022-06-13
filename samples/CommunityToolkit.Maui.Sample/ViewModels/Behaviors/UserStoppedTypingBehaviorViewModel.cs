using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class UserStoppedTypingBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	string performedSearches = string.Empty;

	[RelayCommand]
	void Search(string searchTerms) => PerformedSearches += string.Format($"Performed search for '{searchTerms}'.") + Environment.NewLine;
}