using Microsoft.Maui.Controls.Platform;
using Microsoft.UI.Xaml.Controls;

namespace CommunityToolkit.Maui.Views;

public partial class Expander
{
	static void ForceUpdateCellSize(CollectionView collectionView, Size size, Point? tapLocation)
	{
		if (tapLocation is null)
		{
			return;
		}

		if (collectionView.Handler?.PlatformView is FormsListView formsListView)
		{
			var offset = 0.0;
			foreach (var item in formsListView.Items)
			{
				if (formsListView.ContainerFromItem(item) is ListViewItem cell)
				{
					if (tapLocation.Value.Y >= offset && tapLocation.Value.Y <= offset + cell.ActualHeight)
					{
						cell.Height = size.Height;
						offset += cell.Height - cell.ActualHeight;
					}

					offset += cell.ActualHeight;
				}
			}
		}
		else if (collectionView.Handler?.PlatformView is FormsGridView gridView)
		{
			var numberOfColumns = gridView.Span;
			if (numberOfColumns == 0)
			{
				return;
			}

			for (var i = 0; i < gridView.Items.Count; i++)
			{
				if (gridView.ContainerFromIndex(i) is GridViewItem gridViewItem)
				{
					var itemTransform = gridViewItem.TransformToVisual(gridView);
					var itemPosition = itemTransform.TransformPoint(new Windows.Foundation.Point(0, 0));
					var itemBounds = new Rect(itemPosition.X, itemPosition.Y, gridViewItem.ActualWidth, gridViewItem.ActualHeight);

					if (itemBounds.Contains(tapLocation.Value))
					{
						IterateItemsInRow(gridView, i, numberOfColumns, size.Height);
						break;
					}
				}
			}
		}
	}

	static void IterateItemsInRow(ItemsControl gridView, int itemIndex, int totalColumns, double height)
	{
		var rowToIterate = itemIndex / totalColumns;
		var startIndex = rowToIterate * totalColumns;

		for (var i = startIndex; i < startIndex + totalColumns; i++)
		{
			if (i >= gridView.Items.Count)
			{
				break;
			}

			if (gridView.ContainerFromIndex(i) is GridViewItem cell)
			{
				cell.Height = height + Random.Shared.NextDouble();
			}
		}
	}
}