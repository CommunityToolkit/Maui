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

		if (currentPage is FlyoutPage flyoutPage)
		{
			return GetCurrentPage(flyoutPage.Detail);
		}

		if (currentPage is Shell { CurrentItem.CurrentItem: IShellSectionController shellSectionController })
		{
			return shellSectionController.PresentedPage;
		}

		if (currentPage is IPageContainer<Page> paigeContainer)
		{
			return GetCurrentPage(paigeContainer.CurrentPage);
		}

		return currentPage;
	}

	internal record struct ParentWindow
	{
		static Page CurrentPage => GetCurrentPage(Application.Current?.MainPage ?? throw new InvalidOperationException($"{nameof(Application.Current.MainPage)} cannot be null."));
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
				if (CurrentPage.GetParentWindow().Handler.PlatformView is null)
				{
					return false;
				}
				return true;
			}
		}
	}
}