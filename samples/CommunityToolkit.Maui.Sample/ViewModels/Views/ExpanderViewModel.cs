using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class ExpanderViewModel : BaseViewModel
{
	public ObservableCollection<ContentCreator> ContentCreators { get; } = [];

	public ExpanderViewModel()
	{
		ContentCreators.Add(new ContentCreator("Brandon Minnick", "https://codetraveler.io/", "https://avatars.githubusercontent.com/u/13558917"));
		ContentCreators.Add(new ContentCreator("Gerald Versluis", "https://blog.verslu.is/", "https://avatars.githubusercontent.com/u/939291"));
		ContentCreators.Add(new ContentCreator("Jay Cho", "https://github.com/JoonghyunCho", "https://avatars.githubusercontent.com/u/14328614"));
		ContentCreators.Add(new ContentCreator("Kym Phillpotts", "https://kymphillpotts.com", "https://avatars.githubusercontent.com/u/1327346"));
		ContentCreators.Add(new ContentCreator("Pedro Jesus", "https://github.com/pictos", "https://avatars.githubusercontent.com/u/20712372"));
		ContentCreators.Add(new ContentCreator("Shaun Lawrence", "https://github.com/bijington", "https://avatars.githubusercontent.com/u/17139988"));
		ContentCreators.Add(new ContentCreator("Vladislav Antonyuk", "https://vladislavantonyuk.azurewebsites.net", "https://avatars.githubusercontent.com/u/33021114"));
	}
}

public record ContentCreator(string Name, string Resource, string Image);