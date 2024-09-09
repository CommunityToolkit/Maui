using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views
{
	public partial class ExpanderViewModel : BaseViewModel
	{
		public ObservableCollection<ContentCreator> ContentCreators { get; } = [.. ContentCreator.GetContentCreators()];
	}
}

namespace CommunityToolkit.Maui.Sample
{
	public record ContentCreator(string Name, string Resource, string Image)
	{
		public static IEnumerable<ContentCreator> GetContentCreators() =>
		[
			new("Brandon Minnick", "https://codetraveler.io/", "https://avatars.githubusercontent.com/u/13558917"),
			new("Gerald Versluis", "https://blog.verslu.is/", "https://avatars.githubusercontent.com/u/939291"),
			new("Jay Cho", "https://github.com/JoonghyunCho", "https://avatars.githubusercontent.com/u/14328614"),
			new("Kym Phillpotts", "https://kymphillpotts.com", "https://avatars.githubusercontent.com/u/1327346"),
			new("Pedro Jesus", "https://github.com/pictos", "https://avatars.githubusercontent.com/u/20712372"),
			new("Shaun Lawrence", "https://github.com/bijington", "https://avatars.githubusercontent.com/u/17139988"),
			new("Vladislav Antonyuk", "https://vladislavantonyuk.azurewebsites.net", "https://avatars.githubusercontent.com/u/33021114"),
		];
	}
}