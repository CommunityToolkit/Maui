using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels;

public abstract class BaseGalleryViewModel : BaseViewModel
{
	protected BaseGalleryViewModel(IEnumerable<SectionModel> items)
	{
		Items = items.OrderBy(x => x.Title).ToList();
	}

	public IReadOnlyList<SectionModel> Items { get; }
}