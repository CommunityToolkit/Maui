using CommunityToolkit.Maui.ApplicationModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public partial class BadgeViewModel : BaseViewModel
{
	readonly IBadge badge;

	public BadgeViewModel(IBadge badge)
	{
		this.badge = badge;
	}

	[RelayCommand]
	void Increment()
	{
		badge.SetCount(badge.GetCount() + 1);
	}

	[RelayCommand]
	void Decrement()
	{
		// Do not allow the badge count to go negative
		badge.SetCount(Math.Max(badge.GetCount() - 1, 0));
	}
}