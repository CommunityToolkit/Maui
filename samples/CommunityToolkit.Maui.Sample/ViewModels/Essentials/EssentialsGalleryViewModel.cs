using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels.Essentials;

public class EssentialsGalleryViewModel() : BaseGalleryViewModel(
[
	SectionModel.Create<AppThemeViewModel>("AppThemeResource", "AppThemeResource provides extension methods and markup extensions that make it easy to assign Light Theme, Dark Theme and Default Theme"),
	SectionModel.Create<BadgeViewModel>("Badge", "Allows the user to set app icon badge count on the home screen"),
	SectionModel.Create<FileSaverViewModel>("FileSaver", "Allows the user to save files to the filesystem"),
	SectionModel.Create<FolderPickerViewModel>("FolderPicker", "Allows picking folders from the file system"),
	SectionModel.Create<SpeechToTextViewModel>("SpeechToText", "Converts speech to text")
]);