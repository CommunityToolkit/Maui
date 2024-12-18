using CommunityToolkit.Maui.ImageSources;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.ImageSources;

public partial class GravatarImageSourceViewModel : BaseViewModel
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CacheValidityTimespan))]
	public partial int CacheValidityInDays { get; set; } = 1;

	[ObservableProperty]
	public partial DefaultImage DefaultGravatarSelected { get; set; } = DefaultImage.MysteryPerson;

	[ObservableProperty]
	public partial string Email { get; set; } = "dsiegel@avantipoint.com";

	[ObservableProperty]
	public partial bool EnableCache { get; set; } = true;

	public TimeSpan CacheValidityTimespan => TimeSpan.FromDays(CacheValidityInDays);

	public IReadOnlyList<DefaultImage> DefaultGravatarItems { get; } =
	[
		DefaultImage.MysteryPerson,
		DefaultImage.FileNotFound,
		DefaultImage.Identicon,
		DefaultImage.MonsterId,
		DefaultImage.Retro,
		DefaultImage.Robohash,
		DefaultImage.Wavatar,
		DefaultImage.Blank
	];
}