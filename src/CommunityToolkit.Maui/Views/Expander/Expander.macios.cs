using CoreGraphics;
using Microsoft.Maui.Controls.Handlers.Items;
using UIKit;

namespace CommunityToolkit.Maui.Views;

public partial class Expander
{
	static void ForceUpdateCellSize(CollectionView collectionView, Size size, Point? tapLocation)
	{
		if (tapLocation is null)
		{
			return;
		}

		var controller = GetController(collectionView);

		if (controller?.CollectionView.CollectionViewLayout is ListViewLayout listViewLayout)
		{
			UpdateListLayout(listViewLayout, tapLocation.Value, size);
		}
		else if (controller?.CollectionView.CollectionViewLayout is GridViewLayout gridViewLayout)
		{
			UpdateGridLayout(collectionView, gridViewLayout, tapLocation.Value, size);
		}
	}

	static UICollectionViewController? GetController(CollectionView collectionView)
	{
		var handler = collectionView.Handler as CollectionViewHandler;
		return handler?.Controller;
	}

	static UICollectionViewCell? GetCellByPoint(UICollectionViewCell[] cells, CGPoint point)
	{
		return cells.FirstOrDefault(cell => cell.Frame.Contains(point));
	}

	static void UpdateListLayout(UICollectionViewLayout layout, Point tapLocation, Size size)
	{
		var cells = layout.CollectionView.VisibleCells.OrderBy(x => x.Frame.Y).ToArray();
		var clickedCell = GetCellByPoint(cells, new CGPoint(tapLocation.X, tapLocation.Y));
		if (clickedCell is null)
		{
			return;
		}

		for (int i = 0; i < cells.Length; i++)
		{
			var cell = cells[i];

			if (i > 0)
			{
				var prevCellFrame = cells[i - 1].Frame;
				cell.Frame = new CGRect(cell.Frame.X, prevCellFrame.Y + prevCellFrame.Height, cell.Frame.Width, cell.Frame.Height);
			}

			if (cell.Equals(clickedCell))
			{
				cell.Frame = new CGRect(cell.Frame.X, cell.Frame.Y, cell.Frame.Width, size.Height);
			}
		}
	}
	
	static void UpdateGridLayout(CollectionView gridView, GridViewLayout gridViewLayout, Point tapLocation, Size size)
	{
		var numberOfColumns = ((GridItemsLayout)gridView.ItemsLayout).Span;
		if (numberOfColumns == 0)
		{
			return;
		}
		
		var cells = gridViewLayout.CollectionView.VisibleCells.OrderBy(x => x.Frame.Y).ThenBy(x=>x.Frame.X).ToArray();
		var clickedCell = GetCellByPoint(cells, new CGPoint(tapLocation.X, tapLocation.Y));
		if (clickedCell is null)
		{
			return;
		}
		
		for (int i = 0; i < cells.Length; i++)
		{
			var cell = cells[i];
			if (cell.Equals(clickedCell))
			{
				IterateItemsInRow(cells, i, numberOfColumns, size.Height);
			}
		}
	}
	
	static void IterateItemsInRow(IReadOnlyList<UICollectionViewCell> cells, int itemIndex, int totalColumns, double height)
	{
		var rowToIterate = itemIndex / totalColumns;
		var startIndex = rowToIterate * totalColumns;

		double y = 0;
		for (var i = startIndex; i < startIndex + totalColumns; i++)
		{
			var cell = cells[i];
			if (i == startIndex)
			{
				y = cell.Frame.Y;
			}
			
			cell.Frame = new CGRect(cell.Frame.X, y, cell.Frame.Width, height);
		}
	}
}