using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class TouchBehaviorCollectionViewMultipleSelectionViewModel : BaseViewModel
{
	public ObservableCollection<ContentCreator> ContentCreators { get; } = [.. ContentCreator.GetContentCreators()];

	[RelayCommand]
	void OnRowTapped(ContentCreator creatorTapped)
	{
		Trace.TraceInformation($"{creatorTapped.Name} Tapped");
	}
}