using CommunityToolkit.Maui.ImageSources;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.ImageSources;

public partial class GravatarImageSourceViewModel : BaseViewModel
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CacheValidityTimespan))]
	int cacheValidityInDays = 1;

	[ObservableProperty]
	DefaultImage defaultGravatarSelected = DefaultImage.MysteryPerson;

	[ObservableProperty]
	string email = "dsiegel@avantipoint.com";

	[ObservableProperty]
	bool enableCache = true;

	public TimeSpan CacheValidityTimespan => TimeSpan.FromDays(CacheValidityInDays);

	public IReadOnlyList<DefaultImage> DefaultGravatarItems { get; } = new[]
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
}