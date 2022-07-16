using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels;

public abstract class BaseGalleryViewModel : BaseViewModel
{
	protected BaseGalleryViewModel(IEnumerable<SectionModel> items)
	{
		foreach (var item in items)
		{
			if (items.Count(x => x.ViewModelType == item.ViewModelType) is not 1)
			{
				throw new InvalidOperationException($"Duplicate {nameof(SectionModel)}.{nameof(SectionModel.ViewModelType)} found for {item.ViewModelType}");
			}
		}

		Items = items.OrderBy(x => x.Title).ToList();
	}

	public IReadOnlyList<SectionModel> Items { get; }
}