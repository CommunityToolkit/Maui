using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Layouts;

namespace CommunityToolkit.Maui.Sample.ViewModels.Layouts;

public class LayoutsGalleryViewModel : BaseGalleryViewModel
{
	public LayoutsGalleryViewModel()
		: base(new[]
		{
			SectionModel.Create<StateContainerViewModel>(nameof(StateContainerPage),
				"Properties enabling any Layout derived element to become state-aware"),

			SectionModel.Create<UniformItemsLayoutViewModel>(nameof(UniformItemsLayoutPage),
				"A Grid where all rows and columns have the same size"),
		})
	{
	}
}