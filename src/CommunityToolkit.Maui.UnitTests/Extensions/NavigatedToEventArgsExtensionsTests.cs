using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.UnitTests.Services;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class NavigatedToEventArgsExtensionsTests : BaseViewTest
{
	[Fact]
	public async Task NavigatedToEventArgsExtensions_WasPreviousPageACommunityToolkitPopupPage_ShouldReturnTrue()
	{
		// Arrange
		TaskCompletionSource<bool?> wasPreviousPageACommunityToolkitPopupPageTCS = new();
		var application = (MockApplication)ServiceProvider.GetRequiredService<IApplication>();
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var shell = (Shell)(application.Windows[0].Page ?? throw new InvalidOperationException("Unable to retrieve Shell"));
		var mainPage = shell.CurrentPage;
		var shellContentPage = new ShellContentPage();
		shellContentPage.NavigatedToEventArgsReceived += HandleNavigatedToEventArgsReceived;

		var shellParameters = new Dictionary<string, object>
		{
			{ nameof(ContentPage.BackgroundColor), Colors.Orange }
		};


		// Act
		await mainPage.Navigation.PushAsync(shellContentPage);
		await popupService.ShowPopupAsync<ShortLivedMockPageViewModel>(shell, null, shellParameters, TestContext.Current.CancellationToken);
		var wasPreviousPageACommunityToolkitPopupPage = await wasPreviousPageACommunityToolkitPopupPageTCS.Task;

		// Assert
		Assert.True(wasPreviousPageACommunityToolkitPopupPage);

		void HandleNavigatedToEventArgsReceived(object? sender, NavigatedToEventArgs e)
		{
			if (e.PreviousPage != mainPage)
			{
				wasPreviousPageACommunityToolkitPopupPageTCS.SetResult(e.WasPreviousPageACommunityToolkitPopupPage());
			}
		}
	}

	[Fact]
	public async Task NavigatedToEventArgsExtensions_WasPreviousPageACommunityToolkitPopupPage_ShouldReturnFalse()
	{
		// Arrange
		TaskCompletionSource<bool?> wasPreviousPageACommunityToolkitPopupPageTCS = new();
		var application = (MockApplication)ServiceProvider.GetRequiredService<IApplication>();

		var shell = (Shell)(application.Windows[0].Page ?? throw new InvalidOperationException("Unable to retrieve Shell"));
		var mainPage = shell.CurrentPage;
		var shellContentPage = new ShellContentPage();
		shellContentPage.NavigatedToEventArgsReceived += HandleNavigatedToEventArgsReceived;

		// Act
		await mainPage.Navigation.PushAsync(shellContentPage);
		var wasPreviousPageACommunityToolkitPopupPage = await wasPreviousPageACommunityToolkitPopupPageTCS.Task;

		// Assert
		Assert.False(wasPreviousPageACommunityToolkitPopupPage);

		void HandleNavigatedToEventArgsReceived(object? sender, NavigatedToEventArgs e)
		{
			wasPreviousPageACommunityToolkitPopupPageTCS.SetResult(e.WasPreviousPageACommunityToolkitPopupPage());
		}
	}

	sealed class ShellContentPage : ContentPage
	{
		public event EventHandler<NavigatedToEventArgs>? NavigatedToEventArgsReceived;

		protected override void OnNavigatedTo(NavigatedToEventArgs args)
		{
			base.OnNavigatedTo(args);
			NavigatedToEventArgsReceived?.Invoke(this, args);
		}
	}
}