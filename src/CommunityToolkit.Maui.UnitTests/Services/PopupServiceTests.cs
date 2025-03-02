using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Services;
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

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		await Assert.ThrowsAsync<InvalidOperationException>(() => popupService.ShowPopupAsync<INotifyPropertyChanged>(page.Navigation, PopupOptions.Empty, TestContext.Current.CancellationToken));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenExpired()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await Task.Delay(100, CancellationToken.None);

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		await Assert.ThrowsAsync<OperationCanceledException>(() => popupService.ShowPopupAsync<MockPageViewModel>(page.Navigation, PopupOptions.Empty, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenCanceled()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<OperationCanceledException>(() => popupService.ShowPopupAsync<MockPageViewModel>(Application.Current!.Windows[0].Page!.Navigation, PopupOptions.Empty, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncShouldValidateProperBindingContext()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var popupInstance = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();
		var popupViewModel = ServiceProvider.GetRequiredService<MockPageViewModel>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		await popupService.ShowPopupAsync<MockPageViewModel, object?>(page.Navigation, PopupOptions.Empty, TestContext.Current.CancellationToken);

		Assert.Same(popupInstance.BindingContext, popupViewModel);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncShouldReturnResultOnceClosed()
	{
		var mockPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		var result = await popupService.ShowPopupAsync<MockPageViewModel, object?>(page.Navigation, PopupOptions.Empty, CancellationToken.None);

		Assert.Same(mockPopup.Result, result.Result);
	}

	[Fact]
	public async Task ShowPopupAsync_ShouldThrowArgumentNullException_WhenNavigationIsNull()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ShowPopupAsync<MockPageViewModel>(null, PopupOptions.Empty, CancellationToken.None));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public async Task ShowPopupAsync_ShouldThrowArgumentNullException_WhenPopupOptionsIsNull()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ShowPopupAsync<MockPageViewModel>(page.Navigation, null, CancellationToken.None));
	}

	[Fact]
	public async Task ShowPopupAsync_ShouldThrowArgumentNullException_WhenViewModelIsNull()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ShowPopupAsync<object>(page.Navigation, PopupOptions.Empty, CancellationToken.None));
	}

	[Fact]
	public async Task ShowPopupAsync_ShouldReturnDefaultResult_WhenPopupIsClosedWithoutResult()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var mockPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		var result = await popupService.ShowPopupAsync<MockPageViewModel, object?>(page.Navigation, PopupOptions.Empty, CancellationToken.None);

		result.Result.Should().BeNull();
	}
}

sealed class MockSelfClosingPopup : Popup<object?>
{
	public MockSelfClosingPopup(MockPageViewModel viewModel, object? result = null)
	{
		BindingContext = viewModel;
		Result = result;
		Opened += MockSelfClosingPopupOpened;
	}

	void MockSelfClosingPopupOpened(object? sender, EventArgs e)
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
