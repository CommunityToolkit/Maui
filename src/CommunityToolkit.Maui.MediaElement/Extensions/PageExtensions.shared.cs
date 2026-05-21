using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.Controls;
namespace CommunityToolkit.Maui.Extensions;

// Since MediaElement can't access .NET MAUI internals we have to copy this code here
// https://github.com/dotnet/maui/blob/main/src/Controls/src/Core/Platform/PageExtensions.cs
static class PageExtensions
{
	internal static Page GetCurrentPage(this Window window) => GetCurrentPage(window.Page ?? throw new InvalidOperationException($"{nameof(Page)} cannot be null."));

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

	internal static bool TryGetCurrentPages([NotNullWhen(true)] out IReadOnlyList<Page>? currentPages)
	{
		currentPages = null;

		if (Application.Current?.Windows is not IReadOnlyList<Window> windows)
		{
			return false;
		}

		if (windows.Count is 0)
		{
			throw new InvalidOperationException("Unable to find active Window");
		}

		List<Page> pages = [];
		foreach (var window in windows)
		{
			if (window.Page is null)
			{
				continue;
			}

			pages.Add(window.GetCurrentPage());
		}

		if (pages.Count is 0)
		{
			return false;
		}

		currentPages = pages;
		return true;
	}

	internal record struct ParentWindow
	{
		static Page CurrentPage => (Application.Current?.Windows[^1] ?? throw new InvalidOperationException($"{nameof(Window)} cannot be null.")).GetCurrentPage();
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