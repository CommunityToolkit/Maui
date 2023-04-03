using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public class EssentialsGalleryViewModel : BaseGalleryViewModel
{
	public EssentialsGalleryViewModel()
		: base(new[]
		{
			SectionModel.Create<BadgeCounterViewModel>("BadgeCounter", "Allows the user to set badge counter."),
			SectionModel.Create<FileSaverViewModel>("FileSaver", "Allows the user to save files to the filesystem"),
			SectionModel.Create<FolderPickerViewModel>("FolderPicker", "Allows picking folders from the file system")
		})
	{
	}
}