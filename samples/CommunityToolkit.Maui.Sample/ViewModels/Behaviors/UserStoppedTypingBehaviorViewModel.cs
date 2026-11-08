using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class UserStoppedTypingBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string PerformedSearches { get; set; } = string.Empty;

	// The newest search comes first, so it shows right below the SearchBar however long the log gets
	[RelayCommand]
	void Search(string searchTerms) => PerformedSearches = $"Performed search for '{searchTerms}'." + Environment.NewLine + PerformedSearches;
}