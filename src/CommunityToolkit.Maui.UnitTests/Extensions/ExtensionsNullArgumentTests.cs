using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

#pragma warning disable CS8604, CS8625 // Possible null reference argument; cannot convert null literal to non-nullable reference type.
public class ExtensionsNullArgumentTests : BaseTest
{
	[Fact]
	public void UseMauiCommunityToolkit_NullBuilder_ShouldThrowArgumentNullException()
	{
		MauiAppBuilder? builder = null;

		NullArgumentAssert.Throws("builder", () => builder.UseMauiCommunityToolkit());
	}

	[Fact]
	public void SetAppThemeColor_NullArguments_ShouldThrowArgumentNullException()
	{
		var label = new Label();
		var appThemeColor = new AppThemeColor();

		NullArgumentAssert.Throws("self", () => AppThemeObjectExtensions.SetAppThemeColor(null, Label.TextColorProperty, appThemeColor));
		NullArgumentAssert.Throws("targetProperty", () => label.SetAppThemeColor(null, appThemeColor));
		NullArgumentAssert.Throws("appThemeColor", () => label.SetAppThemeColor(Label.TextColorProperty, null));
	}

	[Fact]
	public void SetAppTheme_NullArguments_ShouldThrowArgumentNullException()
	{
		var label = new Label();
		var appThemeObject = new AppThemeObject();

		NullArgumentAssert.Throws("self", () => AppThemeObjectExtensions.SetAppTheme<object>(null, Label.TextProperty, appThemeObject));
		NullArgumentAssert.Throws("targetProperty", () => label.SetAppTheme(null, appThemeObject));
		NullArgumentAssert.Throws("appThemeResource", () => label.SetAppTheme<object>(Label.TextProperty, null));
	}

	[Fact]
	public void NavigationEventArgsExtensions_NullArgs_ShouldThrowArgumentNullException()
	{
		NullArgumentAssert.Throws("args", () => NavigationEventArgsExtensions.IsDestinationPageACommunityToolkitPopupPage((NavigatedFromEventArgs?)null));
		NullArgumentAssert.Throws("args", () => NavigationEventArgsExtensions.WasPreviousPageACommunityToolkitPopupPage(null));
		NullArgumentAssert.Throws("args", () => NavigationEventArgsExtensions.IsDestinationPageACommunityToolkitPopupPage((NavigatingFromEventArgs?)null));
	}

	[Fact]
	public void ShowPopup_NullArguments_ShouldThrowArgumentNullException()
	{
		var page = new ContentPage();
		var shell = new Shell();
		var view = new Grid();

		NullArgumentAssert.Throws("page", () => PopupExtensions.ShowPopup((Page?)null, view));
		NullArgumentAssert.Throws("view", () => page.ShowPopup(null));
		NullArgumentAssert.Throws("navigation", () => PopupExtensions.ShowPopup((INavigation?)null, view));
		NullArgumentAssert.Throws("view", () => page.Navigation.ShowPopup(null));
		NullArgumentAssert.Throws("shell", () => PopupExtensions.ShowPopup((Shell?)null, view));
		NullArgumentAssert.Throws("view", () => shell.ShowPopup(null));
	}

	[Fact]
	public void ShowPopupAsync_NullArguments_ShouldThrowArgumentNullExceptionBeforeItReturnsTheTask()
	{
		var page = new ContentPage();
		var shell = new Shell();
		var view = new Grid();
		var token = TestContext.Current.CancellationToken;

		NullArgumentAssert.Throws("page", () => { _ = PopupExtensions.ShowPopupAsync((Page?)null, view, null, token); });
		NullArgumentAssert.Throws("view", () => { _ = page.ShowPopupAsync(null, null, token); });
		NullArgumentAssert.Throws("navigation", () => { _ = PopupExtensions.ShowPopupAsync((INavigation?)null, view, null, token); });
		NullArgumentAssert.Throws("view", () => { _ = page.Navigation.ShowPopupAsync(null, null, token); });
		NullArgumentAssert.Throws("shell", () => { _ = PopupExtensions.ShowPopupAsync((Shell?)null, view, null, null, token); });
		NullArgumentAssert.Throws("view", () => { _ = shell.ShowPopupAsync(null, null, null, token); });
	}

	[Fact]
	public void ShowPopupAsyncOfTResult_NullArguments_ShouldThrowArgumentNullExceptionBeforeItReturnsTheTask()
	{
		var page = new ContentPage();
		var shell = new Shell();
		var view = new Grid();
		var token = TestContext.Current.CancellationToken;

		NullArgumentAssert.Throws("page", () => { _ = PopupExtensions.ShowPopupAsync<bool>((Page?)null, view, null, token); });
		NullArgumentAssert.Throws("view", () => { _ = page.ShowPopupAsync<bool>(null, null, token); });
		NullArgumentAssert.Throws("navigation", () => { _ = PopupExtensions.ShowPopupAsync<bool>((INavigation?)null, view, null, token); });
		NullArgumentAssert.Throws("view", () => { _ = page.Navigation.ShowPopupAsync<bool>(null, null, token); });
		NullArgumentAssert.Throws("shell", () => { _ = PopupExtensions.ShowPopupAsync<bool>((Shell?)null, view, null, null, token); });
		NullArgumentAssert.Throws("view", () => { _ = shell.ShowPopupAsync<bool>(null, null, null, token); });
	}

	[Fact]
	public void ClosePopupAsync_NullArguments_ShouldThrowArgumentNullExceptionBeforeItReturnsTheTask()
	{
		var token = TestContext.Current.CancellationToken;

		NullArgumentAssert.Throws("page", () => { _ = PopupExtensions.ClosePopupAsync((Page?)null, token); });
		NullArgumentAssert.Throws("navigation", () => { _ = PopupExtensions.ClosePopupAsync((INavigation?)null, token); });
		NullArgumentAssert.Throws("page", () => { _ = PopupExtensions.ClosePopupAsync((Page?)null, 2, token); });
		NullArgumentAssert.Throws("navigation", () => { _ = PopupExtensions.ClosePopupAsync((INavigation?)null, 2, token); });
	}

	[Fact]
	public void AddPopup_NullServices_ShouldThrowArgumentNullException()
	{
		NullArgumentAssert.Throws("services", () => ServiceCollectionExtensions.AddTransientPopup<NullArgumentPopup>(null));
		NullArgumentAssert.Throws("services", () => ServiceCollectionExtensions.AddTransientPopup<NullArgumentPopup, MockPageViewModel>(null));
		NullArgumentAssert.Throws("services", () => ServiceCollectionExtensions.AddSingletonPopup<NullArgumentPopup>(null));
		NullArgumentAssert.Throws("services", () => ServiceCollectionExtensions.AddSingletonPopup<NullArgumentPopup, MockPageViewModel>(null));
		NullArgumentAssert.Throws("services", () => ServiceCollectionExtensions.AddScopedPopup<NullArgumentPopup>(null));
		NullArgumentAssert.Throws("services", () => ServiceCollectionExtensions.AddScopedPopup<NullArgumentPopup, MockPageViewModel>(null));
	}

	[Fact]
	public void AddViewAndViewModel_NullServices_ShouldThrowArgumentNullException()
	{
		NullArgumentAssert.Throws("services", () => ServiceCollectionExtensions.AddTransient<MockPage, MockPageViewModel>(null));
		NullArgumentAssert.Throws("services", () => ServiceCollectionExtensions.AddSingleton<MockPage, MockPageViewModel>(null));
		NullArgumentAssert.Throws("services", () => ServiceCollectionExtensions.AddScoped<MockPage, MockPageViewModel>(null));
	}

	[Fact]
	public void AddWithShellRoute_NullArguments_ShouldThrowArgumentNullException()
	{
		var services = MauiApp.CreateBuilder().Services;

		NullArgumentAssert.Throws("services", () => ServiceCollectionExtensions.AddTransientWithShellRoute<MockPage, MockPageViewModel>(null, "//NullArgumentRoute"));
		NullArgumentAssert.Throws("route", () => services.AddTransientWithShellRoute<MockPage, MockPageViewModel>(null));
		NullArgumentAssert.Throws("services", () => ServiceCollectionExtensions.AddSingletonWithShellRoute<MockPage, MockPageViewModel>(null, "//NullArgumentRoute"));
		NullArgumentAssert.Throws("route", () => services.AddSingletonWithShellRoute<MockPage, MockPageViewModel>(null));
		NullArgumentAssert.Throws("services", () => ServiceCollectionExtensions.AddScopedWithShellRoute<MockPage, MockPageViewModel>(null, "//NullArgumentRoute"));
		NullArgumentAssert.Throws("route", () => services.AddScopedWithShellRoute<MockPage, MockPageViewModel>(null));
	}

	sealed class NullArgumentPopup : View;
}
#pragma warning restore CS8604, CS8625
