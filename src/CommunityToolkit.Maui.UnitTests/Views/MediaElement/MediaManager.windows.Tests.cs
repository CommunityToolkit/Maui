using CommunityToolkit.Maui.Extensions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class MediaManagerWindowsTests : BaseHandlerTest
{
	public record struct ParentWindow
	{
		static Page CurrentPage =>
		PageExtensions.GetCurrentPage(Application.Current?.MainPage ?? throw new InvalidOperationException($"{nameof(Application.Current.MainPage)} cannot be null."));
		/// <summary>
		/// Checks if the parent window is null.
		/// </summary>
		public static bool Exists
		{
			get
			{
				if (CurrentPage.GetParentWindow() is null)
				{
					System.Diagnostics.Trace.TraceError("Parent window is null");
					return false;
				}
				if (CurrentPage.GetParentWindow().Handler is null)
				{
					System.Diagnostics.Trace.TraceError("Parent window handler is null");
					return false;
				}
				if (CurrentPage.GetParentWindow().Handler.PlatformView is null)
				{
					System.Diagnostics.Trace.TraceError("Parent window handler platform view is null");
					return false;
				}
				return true;
			}
		}
	}

	[Fact]
    public void ParentWindow_Exists_ReturnsFalse_WhenCurrentPageIsNull()
    {
        Application.Current = null;

        Assert.Throws<InvalidOperationException>(() => ParentWindow.Exists);
    }

    [Fact]
    public void ParentWindow_Exists_ReturnsFalse_WhenParentWindowIsNull()
    {
        var mockApplication = new Application();
        var mockPage = new ContentPage();
        mockApplication.MainPage = mockPage;
        Application.Current = mockApplication;

        Assert.False(ParentWindow.Exists);
    }

    [Fact]
    public void ParentWindow_Exists_ReturnsFalse_WhenParentWindowHandlerIsNull()
    {
        var mockApplication = new Application();
        var mockPage = new ContentPage();
        mockApplication.MainPage = mockPage;
        Application.Current = mockApplication;

        // Simulate a non-null parent window with null handler
        mockPage.Parent = new Window();

        Assert.False(ParentWindow.Exists);
    }

    [Fact]
    public void ParentWindow_Exists_ReturnsFalse_WhenParentWindowHandlerPlatformViewIsNull()
    {
		var mockApplication = new Application();
		var mockPage = new ContentPage();
		mockApplication.MainPage = mockPage;
		Application.Current = mockApplication;

		// Simulate a non-null parent window with non-null handler but null platform view
		var mockWindow = new Window();
        mockPage.Parent = mockWindow;
        mockWindow.Handler = null;

        Assert.False(ParentWindow.Exists);
    }
}
