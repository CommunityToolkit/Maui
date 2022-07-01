using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.ViewControls;

/// <summary>Sample Avatar control view model.</summary>
public sealed partial class AvatarControlViewModel : BaseViewModel
{
	public ObservableCollection<AvatarModel> AvatarList { get; }

	public AvatarControlViewModel()
	{
		AvatarList = new ObservableCollection<AvatarModel>(AvatarData.Avatars);
	}
}