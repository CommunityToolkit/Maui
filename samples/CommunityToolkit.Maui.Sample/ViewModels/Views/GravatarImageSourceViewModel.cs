namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class GravatarImageSourceViewModel : BaseViewModel
{
	[ObservableProperty]
	string email = "dsiegel@avantipoint.com";

	[ObservableProperty]
	bool enableCache = true;

	[ObservableProperty]
	DefaultImage defaultGravatarSelected = DefaultImage.MysteryPerson;

	[ObservableProperty]
	DefaultImage[] defaultGravatarItems = new[]
	{
		DefaultImage.MysteryPerson,
		DefaultImage.FileNotFound,
		DefaultImage.Identicon,
		DefaultImage.MonsterId,
		DefaultImage.Retro,
		DefaultImage.Robohash,
		DefaultImage.Wavatar,
		DefaultImage.Blank
	};

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CacheValidityTimespan))]
	int cacheValidityInDays = 1;

	public TimeSpan CacheValidityTimespan => TimeSpan.FromDays(CacheValidityInDays);
}
