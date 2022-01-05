using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels;

public abstract class BaseGalleryViewModel : BaseViewModel
{
	string filterValue = string.Empty;
	IEnumerable<SectionModel> filteredItems = Enumerable.Empty<SectionModel>();

	public BaseGalleryViewModel()
	{
		Items = CreateItems().OrderBy(x => x.Title).ToList();
		Filter();
	}

	public IReadOnlyList<SectionModel> Items { get; }

	public string FilterValue
	{
		get => filterValue;
		set
		{
			filterValue = value;
			Filter();
		}
	}

	public IEnumerable<SectionModel> FilteredItems
	{
		get => filteredItems;
		private set => SetProperty(ref filteredItems, value);
	}

	protected abstract IEnumerable<SectionModel> CreateItems();

	void Filter() => FilteredItems = string.IsNullOrEmpty(FilterValue)
						? Items
						: Items.Where(item => item.Title.Contains(FilterValue, StringComparison.InvariantCultureIgnoreCase));
}