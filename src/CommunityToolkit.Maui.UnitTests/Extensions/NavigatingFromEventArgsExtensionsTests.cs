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
		TaskCompletionSource<bool?> isDestinationPageACommunityToolkitPopupPageTCS = new();
		var application = (MockApplication)ServiceProvider.GetRequiredService<IApplication>();
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var shell = (Shell)(application.Windows[0].Page ?? throw new InvalidOperationException("Unable to retrieve Shell"));
		var mainPage = shell.CurrentPage;
		var shellContentPage = new ShellContentPage();
		shellContentPage.NavigatingFromEventArgsReceived += HandleNavigatingFromEventArgsReceived;

		var shellParameters = new Dictionary<string, object>
		{
			{ nameof(ContentPage.BackgroundColor), Colors.Orange }
		};

		// Act
		await mainPage.Navigation.PushAsync(shellContentPage);
		await popupService.ShowPopupAsync<ShortLivedMockPageViewModel>(shell, null, shellParameters, TestContext.Current.CancellationToken);
		bool? isDestinationPageACommunityToolkitPopupPage = await isDestinationPageACommunityToolkitPopupPageTCS.Task;

		// Assert
		Assert.True(isDestinationPageACommunityToolkitPopupPage);

		void HandleNavigatingFromEventArgsReceived(object? sender, NavigatingFromEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);

			if (sender is not ShellContentPage)
			{
				shellContentPage.NavigatingFromEventArgsReceived -= HandleNavigatingFromEventArgsReceived;
				isDestinationPageACommunityToolkitPopupPageTCS.SetResult(e.IsDestinationPageACommunityToolkitPopupPage());
			}
		}
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
		shellContentPage.NavigatingFromEventArgsReceived += HandleNavigatingFromEventArgsReceived;

		// Act
		await mainPage.Navigation.PushAsync(shellContentPage);
		await mainPage.Navigation.PushAsync(anotherShellContentPage);
		bool? isDestinationPageACommunityToolkitPopupPage = await isDestinationPageACommunityToolkitPopupPageTCS.Task;

		// Assert
		Assert.False(isDestinationPageACommunityToolkitPopupPage);

		void HandleNavigatingFromEventArgsReceived(object? sender, NavigatingFromEventArgs e)
		{
			ArgumentNullException.ThrowIfNull(sender);

			shellContentPage.NavigatingFromEventArgsReceived -= HandleNavigatingFromEventArgsReceived;
			isDestinationPageACommunityToolkitPopupPageTCS.SetResult(e.IsDestinationPageACommunityToolkitPopupPage());
		}
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