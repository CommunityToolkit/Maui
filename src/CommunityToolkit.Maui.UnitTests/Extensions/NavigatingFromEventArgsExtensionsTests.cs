using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class NavigatingFromEventArgsExtensionsTests : BaseViewTest
{
	[Fact]
	public async Task NavigatingFromEventArgsExtensions_IsDestinationPageACommunityToolkitPopupPage_ShouldReturnTrue()
	{
		// Arrange
		MockApplication application = (MockApplication)ServiceProvider.GetRequiredService<IApplication>();
		IPopupService popupService = ServiceProvider.GetRequiredService<IPopupService>();

		Shell shell = (Shell)(application.Windows[0].Page ?? throw new InvalidOperationException("Unable to retrieve Shell"));
		Page mainPage = shell.CurrentPage;
		ShellContentPage shellContentPage = new();

		Dictionary<string, object> shellParameters = new()
		{
			{ nameof(ContentPage.BackgroundColor), Colors.Orange }
		};

		TaskCompletionSource<bool?> isDestinationPageACommunityToolkitPopupPageTCS = new();
		shellContentPage.NavigatingFromEventArgsReceived += (sender, args) =>
		{
			isDestinationPageACommunityToolkitPopupPageTCS.SetResult(args.IsDestinationPageACommunityToolkitPopupPage());
		};

		// Act
		await mainPage.Navigation.PushAsync(shellContentPage);
		await popupService.ShowPopupAsync<ShortLivedMockPageViewModel>(shell, null, shellParameters, TestContext.Current.CancellationToken);
		bool? isDestinationPageACommunityToolkitPopupPage = await isDestinationPageACommunityToolkitPopupPageTCS.Task;

		// Assert
		Assert.True(isDestinationPageACommunityToolkitPopupPage);
	}

	[Fact]
	public async Task NavigatingFromEventArgsExtensions_IsDestinationPageACommunityToolkitPopupPage_ShouldReturnFalse()
	{
		// Arrange
		MockApplication application = (MockApplication)ServiceProvider.GetRequiredService<IApplication>();
		IPopupService popupService = ServiceProvider.GetRequiredService<IPopupService>();

		Shell shell = (Shell)(application.Windows[0].Page ?? throw new InvalidOperationException("Unable to retrieve Shell"));
		Page mainPage = shell.CurrentPage;

		ShellContentPage shellContentPage = new();
		ShellContentPage anotherShellContentPage = new();

		TaskCompletionSource<bool?> isDestinationPageACommunityToolkitPopupPageTCS = new();
		shellContentPage.NavigatingFromEventArgsReceived += (sender, args) =>
		{
			isDestinationPageACommunityToolkitPopupPageTCS.SetResult(args.IsDestinationPageACommunityToolkitPopupPage());
		};

		// Act
		await mainPage.Navigation.PushAsync(shellContentPage);
		await mainPage.Navigation.PushAsync(anotherShellContentPage);
		bool? isDestinationPageACommunityToolkitPopupPage = await isDestinationPageACommunityToolkitPopupPageTCS.Task;

		// Assert
		Assert.False(isDestinationPageACommunityToolkitPopupPage);
	}

	sealed class ShellContentPage : ContentPage
	{
		public event EventHandler<NavigatingFromEventArgs>? NavigatingFromEventArgsReceived;

		protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
		{
			base.OnNavigatingFrom(args);
			NavigatingFromEventArgsReceived?.Invoke(this, args);
		}
	}
}