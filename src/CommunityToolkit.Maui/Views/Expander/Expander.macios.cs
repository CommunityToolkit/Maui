using System.Reflection;
using CoreGraphics;
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

		if (controller?.CollectionView.CollectionViewLayout is UICollectionViewFlowLayout layout)
		{
			var cells = layout.CollectionView.VisibleCells.OrderBy(x => x.Frame.Y).ToArray();
			var clickedCell = GetCellByPoint(cells, new CGPoint(tapLocation.Value.X, tapLocation.Value.Y));
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
	}

	static UICollectionViewController? GetController(CollectionView collectionView)
	{
		var handler = collectionView.Handler as Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler;
		return handler?.Controller;
	}

	static UICollectionViewCell? GetCellByPoint(UICollectionViewCell[] cells, CGPoint point)
	{
		return cells.FirstOrDefault(cell => cell.Frame.Contains(point));
	}
}