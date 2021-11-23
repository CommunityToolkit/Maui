using System;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class UserStoppedTypingBehaviorViewModel : BaseViewModel
{
	string performedSearches = string.Empty;

	public string PerformedSearches
	{
		get => performedSearches;
		set => SetProperty(ref performedSearches, value);
	}

	public ICommand SearchCommand { get; }

	public UserStoppedTypingBehaviorViewModel() => SearchCommand = new Command<string>(PerformSearch);

	void PerformSearch(string searchTerms) => PerformedSearches += string.Format($"Performed search for '{searchTerms}'.") + Environment.NewLine;
}