using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters
{
	public class ListIsNotNullOrEmptyConverterViewModel : BaseViewModel
	{
		public ListIsNotNullOrEmptyConverterViewModel()
        {
			ClearCollectionCommand = new Command(StringItemSource.Clear);
			StringItemSource.CollectionChanged += HandleCollectionChanged;
		}

        public ICommand ClearCollectionCommand { get; }

		public ObservableCollection<string> StringItemSource { get; } = new()
		{
			"Dummy Item 0",
			"Dummy Item 1",
			"Dummy Item 2",
			"Dummy Item 3",
			"Dummy Item 4",
			"Dummy Item 5",
		};

		void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			OnPropertyChanged(nameof(StringItemSource));
		}
	}
}
