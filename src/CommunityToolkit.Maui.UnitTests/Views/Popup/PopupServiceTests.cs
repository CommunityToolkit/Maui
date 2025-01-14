using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.UnitTests.Views;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class PopupServiceTests : BaseHandlerTest
{
	public PopupServiceTests()
	{
		var page = new MockPage(new MockPageViewModel());
		var serviceCollection = new ServiceCollection();
		PopupService.AddPopup<MockPopup, MockPageViewModel>(serviceCollection, ServiceLifetime.Transient);
		CreateViewHandler<MockPageHandler>(page);

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = page;
	}

	[Fact]
	public async Task ShowPopupAsyncWithNullOnPresentingShouldThrowArgumentNullException()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() =>
			popupService.ShowPopupAsync<INotifyPropertyChanged>(new PopupOptions(), CancellationToken.None));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenExpired()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await Task.Delay(100, CancellationToken.None);

		await Assert.ThrowsAsync<TaskCanceledException>(() => popupService.ShowPopupAsync<MockPageViewModel>(new PopupOptions(), cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenCanceled()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<TaskCanceledException>(() => popupService.ShowPopupAsync<MockPageViewModel>(new PopupOptions(), cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncShouldValidateProperBindingContext()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var popupInstance = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();
		var popupViewModel = ServiceProvider.GetRequiredService<MockPageViewModel>();

		await popupService.ShowPopupAsync<MockPageViewModel>(new PopupOptions(), CancellationToken.None);

		Assert.Same(popupInstance.BindingContext, popupViewModel);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncWithOnPresenting_CancellationTokenExpired()
	{
		var popupViewModel = new MockPageViewModel();
		var popupInstance = new MockSelfClosingPopup(popupViewModel)
		{
			BindingContext = popupViewModel
		};

		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await Task.Delay(100, CancellationToken.None);

		await Assert.ThrowsAsync<TaskCanceledException>(() => popupService.ShowPopupAsync<MockPageViewModel>(new PopupOptions<MockPageViewModel>(){OnOpened = viewModel => viewModel.HasLoaded = true}, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncWithOnPresenting_CancellationTokenCanceled()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<TaskCanceledException>(() => popupService.ShowPopupAsync<MockPageViewModel>(new PopupOptions<MockPageViewModel>() { OnOpened = viewModel => viewModel.HasLoaded = true }, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncWithOnPresentingShouldBeInvoked()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var popupViewModel = ServiceProvider.GetRequiredService<MockPageViewModel>();

		await popupService.ShowPopupAsync<MockPageViewModel>(new PopupOptions<MockPageViewModel>() { OnOpened = viewModel => viewModel.HasLoaded = true }, CancellationToken.None);

		Assert.True(popupViewModel.HasLoaded);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncShouldReturnResultOnceClosed()
	{
		var mockPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var result = await popupService.ShowPopupAsync<MockPageViewModel>(new PopupOptions(), CancellationToken.None);

		Assert.Same(mockPopup.Result, result);
	}

	[Fact]
	public void ShowPopupWithOnPresentingShouldBeInvoked()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var popupViewModel = ServiceProvider.GetRequiredService<MockPageViewModel>();

		popupService.ShowPopupAsync<MockPageViewModel>(new PopupOptions<MockPageViewModel>() { OnOpened = viewModel => viewModel.HasLoaded = true }, CancellationToken.None);

		Assert.True(popupViewModel.HasLoaded);
	}
}

sealed class MockSelfClosingPopup : Popup
{
	public MockSelfClosingPopup(MockPageViewModel viewModel, object? result = null)
	{
		BindingContext = viewModel;
		Result = result;
	}

	public new object? Result { get; }
}