using PlatformGridView = Microsoft.UI.Xaml.Controls.GridView;
using PlatformGridViewItem = Microsoft.UI.Xaml.Controls.GridViewItem;
using PlatformItemsControl = Microsoft.UI.Xaml.Controls.ItemsControl;
using PlatformListView = Microsoft.UI.Xaml.Controls.ListView;
using PlatformListViewItem = Microsoft.UI.Xaml.Controls.ListViewItem;

namespace CommunityToolkit.Maui.Views;

public partial class Expander
{
	static void ForceUpdateCellSize(CollectionView collectionView, Size size, Point? tapLocation)
	{
		if (tapLocation is null)
		{
			return;
		}

		if (collectionView.Handler?.PlatformView is PlatformListView listView)
		{
			foreach (var item in listView.Items)
			{
				if (listView.ContainerFromItem(item) is PlatformListViewItem cell)
				{
					// Where the list shows the container (horizontal or vertical, scrolled or not), in the coordinates of the tap location
					var cellTransform = cell.TransformToVisual(listView);
					var cellPosition = cellTransform.TransformPoint(new Windows.Foundation.Point(0, 0));
					var cellBounds = new Rect(cellPosition.X, cellPosition.Y, cell.ActualWidth, cell.ActualHeight);

					if (cellBounds.Contains(tapLocation.Value))
					{
						cell.Height = size.Height;
						break;
					}
				}
			}
		}
		else if (collectionView.Handler?.PlatformView is PlatformGridView gridView)
		{
			if (collectionView.ItemsLayout is not GridItemsLayout { Span: > 0 } gridItemsLayout)
			{
				return;
			}

			for (var i = 0; i < gridView.Items.Count; i++)
			{
				if (gridView.ContainerFromIndex(i) is PlatformGridViewItem gridViewItem)
				{
					var itemTransform = gridViewItem.TransformToVisual(gridView);
					var itemPosition = itemTransform.TransformPoint(new Windows.Foundation.Point(0, 0));
					var itemBounds = new Rect(itemPosition.X, itemPosition.Y, gridViewItem.ActualWidth, gridViewItem.ActualHeight);

					if (itemBounds.Contains(tapLocation.Value))
					{
						IterateItemsInRow(gridView, i, gridItemsLayout.Span, size.Height);
						break;
					}
				}
			}
		}
	}

	static void IterateItemsInRow(PlatformItemsControl gridView, int itemIndex, int totalColumns, double height)
	{
		var rowToIterate = itemIndex / totalColumns;
		var startIndex = rowToIterate * totalColumns;

		for (var i = startIndex; i < startIndex + totalColumns; i++)
		{
			if (i >= gridView.Items.Count)
			{
				break;
			}

			if (gridView.ContainerFromIndex(i) is PlatformGridViewItem cell)
			{
				cell.Height = height + Random.Shared.NextDouble();
			}
		}
	}
}