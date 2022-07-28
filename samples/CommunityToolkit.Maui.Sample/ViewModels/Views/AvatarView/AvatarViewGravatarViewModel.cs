namespace CommunityToolkit.Maui.Sample.ViewModels.Views.AvatarView;

using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class AvatarViewGravatarViewModel : BaseViewModel
{
	[ObservableProperty]
	string email = "dsiegel@avantipoint.com";

	[ObservableProperty]
	bool enableCache = true;

	[ObservableProperty]
	DefaultGravatarImage defaultGravatarSelected = DefaultGravatarImage.MysteryPerson;

	[ObservableProperty]
	DefaultGravatarImage[] defaultGravatarItems = new[]
	{
		DefaultGravatarImage.MysteryPerson,
		DefaultGravatarImage.FileNotFound,
		DefaultGravatarImage.Identicon,
		DefaultGravatarImage.MonsterId,
		DefaultGravatarImage.Retro,
		DefaultGravatarImage.Robohash,
		DefaultGravatarImage.Wavatar,
		DefaultGravatarImage.Blank
	};

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CacheValidityTimespan))]
	int cacheValidityInDays = 1;

	public TimeSpan CacheValidityTimespan => TimeSpan.FromDays(CacheValidityInDays);
}