using CommunityToolkit.Maui.Core.Extensions;
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
	}
}