using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class UserStoppedTypingBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string PerformedSearches { get; set; } = string.Empty;

	[RelayCommand]
	void Search(string searchTerms) => PerformedSearches += string.Format($"Performed search for '{searchTerms}'.") + Environment.NewLine;
}