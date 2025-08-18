using CommunityToolkit.Maui.Extensions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;
public class PageExtensionsTests : BaseTest
{
	[Fact]
	public void GetCurrentPage_ReturnsModalPage_WhenModalStackIsNotEmpty()
	{
		// Arrange
		var modalPage = new ContentPage();
		var navigationPage = new NavigationPage(new ContentPage());
		navigationPage.Navigation.PushModalAsync(modalPage);

		// Act
		var result = PageExtensions.GetCurrentPage(navigationPage);

		// Assert
		Assert.Equal(modalPage, result);
	}

	[Fact]
	public void GetCurrentPage_ReturnsFlyoutDetailPage_WhenCurrentPageIsFlyoutPage()
	{
		// Arrange
		var detailPage = new ContentPage();
		var flyoutPage = new FlyoutPage
		{
			Detail = new NavigationPage(detailPage),
			Flyout = new ContentPage()
			{
				Title = "Flyout",
			}
		};

		// Act
		var result = PageExtensions.GetCurrentPage(flyoutPage);

		// Assert
		Assert.Equal(detailPage, result);
	}

	[Fact]
	public void GetCurrentPage_ReturnsShellPresentedPage_WhenCurrentPageIsShell()
	{
		// Arrange
		var presentedPage = new ContentPage();
		var shell = new Shell();
		var shellSection = new ShellSection
		{
			Items = { new ShellContent { Content = presentedPage } }
		};
		shell.Items.Add(new ShellItem { Items = { shellSection } });
		shell.CurrentItem = shell.Items[0];

		// Act
		var result = PageExtensions.GetCurrentPage(shell);

		// Assert
		Assert.Equal(presentedPage, result);
	}

	[Fact]
	public void GetCurrentPage_ReturnsContainerCurrentPage_WhenCurrentPageIsPageContainer()
	{
		var dispatcher = DispatcherProvider.Current.GetForCurrentThread() ?? throw new InvalidOperationException("Dispatcher is not available for the current thread.");
		// Arrange
		var currentPage = new ContentPage();

		TabbedPage? pageContainer = null;
		dispatcher.Dispatch(() =>
		{
			pageContainer = new TabbedPage
			{
				Children =
				{
					currentPage,
				}
			};
		});
		if (pageContainer is null)
		{
			throw new InvalidOperationException("PageContainer is null");
		}
		// Act
		var result = PageExtensions.GetCurrentPage(pageContainer);

		// Assert
		Assert.Equal(currentPage, result);
	}

	[Fact]
	public void GetCurrentPage_ReturnsCurrentPage_WhenNoSpecialCasesApply()
	{
		// Arrange
		var currentPage = new ContentPage();

		// Act
		var result = PageExtensions.GetCurrentPage(currentPage);

		// Assert
		Assert.Equal(currentPage, result);
	}
}
