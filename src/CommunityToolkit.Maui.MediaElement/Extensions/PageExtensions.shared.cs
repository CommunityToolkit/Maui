using Microsoft.Maui.Controls;
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
		else if (currentPage is FlyoutPage fp)
		{
			return GetCurrentPage(fp.Detail);
		}
		else if (currentPage is Shell shell && shell.CurrentItem?.CurrentItem is IShellSectionController ssc)
		{
			return ssc.PresentedPage;
		}
		else if (currentPage is IPageContainer<Page> pc)
		{
			return GetCurrentPage(pc.CurrentPage);
		}
		else
		{
			return currentPage;
		}
	}

	internal record struct ParentWindow
	{
		static Page CurrentPage => GetCurrentPage(Application.Current?.Windows[^1].Page ?? throw new InvalidOperationException($"{nameof(Page)} cannot be null."));
		/// <summary>
		/// Checks if the parent window is null.
		/// </summary>
		public static bool Exists
		{
			get
			{
				if (CurrentPage.GetParentWindow() is null)
				{
					return false;
				}
				if (CurrentPage.GetParentWindow().Handler is null)
				{
					return false;
				}

				return CurrentPage.GetParentWindow().Handler?.PlatformView is not null;
			}
		}
	}
}