using Microsoft.Maui.Controls;
using System;
using System.Windows.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors
{
    public class UserStoppedTypingBehaviorViewModel : BaseViewModel
    {
        string performedSearches = string.Empty;

        public string PerformedSearches
        {
            get => performedSearches;
            set => SetProperty(ref performedSearches, value);
        }

        public ICommand SearchCommand { get; }

        public UserStoppedTypingBehaviorViewModel()
            => SearchCommand = new Command<string>(PerformSearch);

        void PerformSearch(string searchTerms)
            => PerformedSearches += string.Format("Performed search for '{0}'.", searchTerms) + Environment.NewLine;
    }
}