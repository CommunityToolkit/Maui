using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.UnitTests.Services;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class NavigatedFromEventArgsExtensionsTests : BaseViewTest
{
	[Fact]
	public async Task NavigatedFromEventArgsExtensions_IsDestinationPageACommunityToolkitPopupPage_ShouldReturnTrue()
	{
		// Arrange
		TaskCompletionSource<bool?> isDestinationPageACommunityToolkitPopupPageTCS = new();
		var application = (MockApplication)ServiceProvider.GetRequiredService<IApplication>();
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var shell = (Shell)(application.Windows[0].Page ?? throw new InvalidOperationException("Unable to retrieve Shell"));
		var mainPage = shell.CurrentPage;
		var shellContentPage = new ShellContentPage();
		shellContentPage.NavigatedFromEventArgsReceived += HandleNavigatedFromEventArgsReceived;

		var shellParameters = new Dictionary<string, object>
		{
			{ nameof(ContentPage.BackgroundColor), Colors.Orange }
		};


		// Act
		await mainPage.Navigation.PushAsync(shellContentPage);
		await popupService.ShowPopupAsync<ShortLivedMockPageViewModel>(shell, null, shellParameters, TestContext.Current.CancellationToken);
		var isDestinationPageACommunityToolkitPopupPage = await isDestinationPageACommunityToolkitPopupPageTCS.Task;

		// Assert
		Assert.True(isDestinationPageACommunityToolkitPopupPage);

		void HandleNavigatedFromEventArgsReceived(object? sender, NavigatedFromEventArgs e)
		{
			isDestinationPageACommunityToolkitPopupPageTCS.SetResult(e.IsDestinationPageACommunityToolkitPopupPage());
		}
	}

	[Fact]
	public async Task NavigatedFromEventArgsExtensions_IsDestinationPageACommunityToolkitPopupPage_ShouldReturnFalse()
	{
		// Arrange
		TaskCompletionSource<bool?> isDestinationPageACommunityToolkitPopupPageTCS = new();
		var application = (MockApplication)ServiceProvider.GetRequiredService<IApplication>();

		var shell = (Shell)(application.Windows[0].Page ?? throw new InvalidOperationException("Unable to retrieve Shell"));
		var mainPage = shell.CurrentPage;
		var shellContentPage = new ShellContentPage();
		shellContentPage.NavigatedFromEventArgsReceived += HandleNavigatedFromEventArgsReceived;
		var newShellContentPage = new ShellContentPage();


		// Act
		await mainPage.Navigation.PushAsync(shellContentPage);
		//push a new content page on top to make sure the navigation handler doesn't think we're navigating to a popup page
		await mainPage.Navigation.PushAsync(newShellContentPage);
		var isDestinationPageACommunityToolkitPopupPage = await isDestinationPageACommunityToolkitPopupPageTCS.Task;

		// Assert
		Assert.False(isDestinationPageACommunityToolkitPopupPage);

		void HandleNavigatedFromEventArgsReceived(object? sender, NavigatedFromEventArgs e)
		{
			isDestinationPageACommunityToolkitPopupPageTCS.SetResult(e.IsDestinationPageACommunityToolkitPopupPage());
		}
	}

	sealed class ShellContentPage : ContentPage
	{
		public event EventHandler<NavigatedFromEventArgs>? NavigatedFromEventArgsReceived;

		protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
		{
			base.OnNavigatedFrom(args);
			NavigatedFromEventArgsReceived?.Invoke(this, args);
		}
	}
}