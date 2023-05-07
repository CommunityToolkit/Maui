namespace CommunityToolkit.Maui.Extensions;

// Since MediaElement can't access .NET MAUI internals we have to copy this code here
// https://github.com/dotnet/maui/blob/main/src/Controls/src/Core/Platform/PageExtensions.cs
static class PageExtensions
{
	internal static Page GetCurrentPage(this Page currentPage)
	{
		if (currentPage.NavigationProxy.ModalStack.LastOrDefault() is Page modal)
		{
			return modal;
		}
		if (currentPage is FlyoutPage fp)
		{
			return GetCurrentPage(fp.Detail);
		}
		if (currentPage is Shell shell && shell.CurrentItem?.CurrentItem is IShellSectionController ssc)
		{
			return ssc.PresentedPage;
		}
		if (currentPage is IPageContainer<Page> pc)
		{
			return GetCurrentPage(pc.CurrentPage);
		}

		return currentPage;
	}
}
