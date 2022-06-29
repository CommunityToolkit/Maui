using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels.ViewControls;

public class ControlsGalleryViewModel : BaseGalleryViewModel
{
	public ControlsGalleryViewModel()
		: base(new[]
		{
		SectionModel.Create<AvatarControlViewModel>("Avatar", "A control to display an avatar or the user's initials that reacts to touch events."),
		})
	{
	}
}