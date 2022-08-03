using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels;

public abstract class BaseGalleryViewModel : BaseViewModel
{
	protected BaseGalleryViewModel(SectionModel[] items)
	{
		if (DoesItemsArrayContainDuplicates(items, out var duplicatedSectionModels))
		{
			throw new InvalidOperationException($"Duplicate {nameof(SectionModel)}.{nameof(SectionModel.ViewModelType)} found for {duplicatedSectionModels.First().ViewModelType}");
		}

		Items = items.OrderBy(x => x.Title).ToList();
	}

	public IReadOnlyList<SectionModel> Items { get; }

	static bool DoesItemsArrayContainDuplicates(in SectionModel[] items, [NotNullWhen(true)] out IReadOnlyList<SectionModel>? duplicatedSectionModels)
	{
		var discoveredDuplicatedSectionModels = new List<SectionModel>();

		var itemsGroupedByViewModelType = items.GroupBy(x => x.ViewModelType);
		foreach (var duplicatedItemsGroups in itemsGroupedByViewModelType.Where(x => x.Count() > 1))
		{
			discoveredDuplicatedSectionModels.AddRange(duplicatedItemsGroups);
		}

		if (discoveredDuplicatedSectionModels.Any())
		{
			duplicatedSectionModels = discoveredDuplicatedSectionModels;
			return true;
		}
		else
		{
			duplicatedSectionModels = null;
			return false;
		}
	}
}