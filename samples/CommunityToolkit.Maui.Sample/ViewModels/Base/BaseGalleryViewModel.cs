using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels;

public abstract class BaseGalleryViewModel : BaseViewModel
{
	public BaseGalleryViewModel()
	{
		Items = CreateItems().OrderBy(x => x.Title).ToList();
	}

	public IReadOnlyList<SectionModel> Items { get; }

	protected abstract IEnumerable<SectionModel> CreateItems();
}