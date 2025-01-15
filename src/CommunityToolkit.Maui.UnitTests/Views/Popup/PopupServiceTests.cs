using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.UnitTests.Views;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Microsoft.Maui.Dispatching;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class PopupServiceTests : BaseHandlerTest
{
	public PopupServiceTests()
	{
		var page = new MockPage(new MockPageViewModel());
		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = page;
	}

	[Fact]
	public async Task ShowPopupAsyncWithNotRegisteredServiceShouldThrowInvalidOperationException()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		await Assert.ThrowsAsync<InvalidOperationException>(() => popupService.ShowPopupAsync(new PopupOptions<INotifyPropertyChanged>(), CancellationToken.None));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenExpired()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await Task.Delay(100, CancellationToken.None);

		await Assert.ThrowsAsync<TaskCanceledException>(() => popupService.ShowPopupAsync(new PopupOptions<MockPageViewModel>(), cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenCanceled()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<TaskCanceledException>(() => popupService.ShowPopupAsync(new PopupOptions<MockPageViewModel>(), cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncShouldValidateProperBindingContext()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var popupInstance = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();
		var popupViewModel = ServiceProvider.GetRequiredService<MockPageViewModel>();

		await popupService.ShowPopupAsync(new PopupOptions<MockPageViewModel>(), CancellationToken.None);

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

		await Assert.ThrowsAsync<TaskCanceledException>(() => popupService.ShowPopupAsync(new PopupOptions<MockPageViewModel>(){OnOpened = viewModel => viewModel.HasLoaded = true}, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncWithOnPresenting_CancellationTokenCanceled()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<TaskCanceledException>(() => popupService.ShowPopupAsync(new PopupOptions<MockPageViewModel>() { OnOpened = viewModel => viewModel.HasLoaded = true }, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncWithOnPresentingShouldBeInvoked()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var popupViewModel = ServiceProvider.GetRequiredService<MockPageViewModel>();

		await popupService.ShowPopupAsync(new PopupOptions<MockPageViewModel>()
		{
			OnOpened = viewModel => viewModel.HasLoaded = true
		}, CancellationToken.None);

		Assert.True(popupViewModel.HasLoaded);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncShouldReturnResultOnceClosed()
	{
		var mockPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var result = await popupService.ShowPopupAsync<MockPageViewModel, object>(new PopupOptions<MockPageViewModel>(), CancellationToken.None);

		Assert.Same(mockPopup.Result, result.Result);
	}

	[Fact]
	public void ShowPopupWithOnPresentingShouldBeInvoked()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var popupViewModel = ServiceProvider.GetRequiredService<MockPageViewModel>();

		popupService.ShowPopupAsync(new PopupOptions<MockPageViewModel>() { OnOpened = viewModel => viewModel.HasLoaded = true }, CancellationToken.None);

		Assert.True(popupViewModel.HasLoaded);
	}
}

sealed class MockSelfClosingPopup : Popup<object?>
{
	public MockSelfClosingPopup(MockPageViewModel viewModel, object? result = null)
	{
		BindingContext = viewModel;
		Result = result;
		OnOpened += MockSelfClosingPopup_OnOpened;
	}

	void MockSelfClosingPopup_OnOpened(object? sender, EventArgs e)
	{
		var timer = Dispatcher.CreateTimer();
		timer.Interval = TimeSpan.FromMilliseconds(500);
		timer.Tick += async (s, e) => await Close(Result);
		timer.Start();
	}

	public object? Result { get; }
}

public class MockPopup : Popup
{
}

class PopupViewModel : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	public Color? Color
	{
		get;
		set
		{
			if (!Equals(value, field))
			{
				field = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
			}
		}
	} = new();
}