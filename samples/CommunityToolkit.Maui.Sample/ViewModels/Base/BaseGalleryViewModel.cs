using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels;

public abstract class BaseGalleryViewModel : BaseViewModel
{
	protected BaseGalleryViewModel(SectionModel[] items)
	{
		foreach (SectionModel? item in from SectionModel item in items
									   where items.Count(x => x.ViewModelType == item.ViewModelType) is not 1
									   select item)
		{
			if (item is not null)
			{
				throw new InvalidOperationException($"Duplicate {nameof(SectionModel)}.{nameof(SectionModel.ViewModelType)} found for {item.ViewModelType}");
			}
		}

		Items = items.OrderBy(x => x.Title).ToList();
	}

	public IReadOnlyList<SectionModel> Items { get; }
}