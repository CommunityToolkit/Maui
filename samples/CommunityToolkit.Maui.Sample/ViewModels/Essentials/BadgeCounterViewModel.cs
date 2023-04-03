using CommunityToolkit.Maui.BadgeCounter;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class BadgeCounterViewModel : BaseViewModel
{
	int count;
	readonly IBadgeCounter badgeCounter;

	public BadgeCounterViewModel(IBadgeCounter badgeCounter)
	{
		this.badgeCounter = badgeCounter;
	}
	
	[RelayCommand]
	void Increment()
	{
		count++;
		badgeCounter.SetBadgeCount(count);
	}
	
	[RelayCommand]
	void Decrement()
	{
		if (count > 0)
		{
			count--;
			badgeCounter.SetBadgeCount(count);
		}
	}
}