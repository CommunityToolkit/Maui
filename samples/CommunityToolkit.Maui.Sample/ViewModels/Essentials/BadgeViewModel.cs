using CommunityToolkit.Maui.ApplicationModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class BadgeViewModel : BaseViewModel
{
	readonly IBadge badge;

	uint count;

	public BadgeViewModel(IBadge badge)
	{
		this.badge = badge;
	}

	[RelayCommand]
	void Increment()
	{
		count++;
		badge.SetCount(count);
	}

	[RelayCommand]
	void Decrement()
	{
		if (count > 0)
		{
			count--;
			badge.SetCount(count);
		}
	}
}