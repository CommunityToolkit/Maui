namespace CommunityToolkit.Maui.Views;

public partial class Expander 
{
	static void ForceUpdateCellSize(Expander expander, CollectionView collectionView, Size size, Point? tapLocation)
	{
		var formsListView = collectionView.Handler?.PlatformView as Microsoft.Maui.Controls.Platform.FormsListView;
		if (formsListView is not null)
		{
			foreach (var item in formsListView.Items)
			{
				var cell = formsListView.ContainerFromItem(item) as Microsoft.UI.Xaml.Controls.ListViewItem;
				if (cell is not null)
				{
					cell.Height = size.Height;
				}
			}
		}
	}
}