using Microsoft.Maui.Controls.Platform;
using Microsoft.UI.Xaml.Controls;

namespace CommunityToolkit.Maui.Views;

public partial class Expander 
{
	static void ForceUpdateCellSize(CollectionView collectionView, Size size, Point? tapLocation)
	{
		if (collectionView.Handler?.PlatformView is FormsListView formsListView)
		{
			foreach (var item in formsListView.Items)
			{
				if (formsListView.ContainerFromItem(item) is ListViewItem cell)
				{
					cell.Height = size.Height;
				}
			}
		}
	}
}