using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels;

public abstract class BaseGalleryViewModel : BaseViewModel
{
    string _filterValue = string.Empty;
    IEnumerable<SectionModel> _filteredItems = Enumerable.Empty<SectionModel>();

    public BaseGalleryViewModel()
    {
        Items = CreateItems().ToList();
        Filter();
    }

    public IReadOnlyList<SectionModel> Items { get; }

    public string FilterValue
    {
        get { return _filterValue; }
        set
        {
            _filterValue = value;
            Filter();
        }
    }

    public IEnumerable<SectionModel> FilteredItems
    {
        get => _filteredItems;
        private set => SetProperty(ref _filteredItems, value);
    }

    protected abstract IEnumerable<SectionModel> CreateItems();

    void Filter() => FilteredItems = string.IsNullOrEmpty(FilterValue)
                        ? Items
                        : Items.Where(item => item.Title.Contains(FilterValue, StringComparison.InvariantCultureIgnoreCase));
}