using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupTests : BaseHandlerTest
{
	const string resultWhenUserTapsOutsideOfPopup = "User Tapped Outside of Popup";
	readonly MockPopup popup = new();
	readonly MockPopupHandler popupHandler;

	public PopupTests()
	{
		popupHandler = CreateElementHandler<MockPopupHandler>(popup);
		Assert.IsType<IPopup>(new MockPopup(), exactMatch: false);
	}

	[Fact]
	public void GetRequiredServiceThrowsOnNoContext()
	{
		var handlerStub = new MockPopupHandler();

		(handlerStub as IElementHandler).MauiContext.Should().BeNull();

		var ex = Assert.Throws<InvalidOperationException>(() => handlerStub.GetRequiredService<IFooService>());

		Assert.Contains("the context", ex.Message);
		Assert.Contains("MauiContext", ex.Message);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenExpired()
	{
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		var app = Application.Current ?? throw new NullReferenceException();

		var page = new ContentPage
		{
			Content = new Label
			{
				Text = "Hello there"
			}
		};

		// Make sure that our page will have a Handler
		CreateViewHandler<MockPageHandler>(page);

		var window = new Window
		{
			Page = page
		};
		app.AddWindow(window);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		// Ensure CancellationToken Has Expired
		await Task.Delay(100, TestContext.Current.CancellationToken);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => page.ShowPopupAsync(popup, cts.Token));
		app.RemoveWindow(window);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenCancelled()
	{
		var cts = new CancellationTokenSource();

		var page = new ContentPage
		{
			Content = new Label
			{
				Text = "Hello there"
			}
		};

		// Make sure that our page will have a Handler
		CreateViewHandler<MockPageHandler>(page);

		var window = new Window
		{
			Page = page
		};
		Application.Current?.AddWindow(window);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		// Ensure CancellationToken Has Expired
		await cts.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => page.ShowPopupAsync(popup, cts.Token));
		Application.Current?.RemoveWindow(window);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CloseAsync_CancellationTokenExpired()
	{
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		var app = Application.Current ?? throw new NullReferenceException();

		var page = new ContentPage
		{
			Content = new Label
			{
				Text = "Hello there"
			}
		};

		// Make sure that our page will have a Handler
		CreateViewHandler<MockPageHandler>(page);

		var window = new Window
		{
			Page = page
		};
		app.AddWindow(window);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		// Ensure CancellationToken Has Expired
		await Task.Delay(100, TestContext.Current.CancellationToken);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => popup.CloseAsync(token: cts.Token));
		app.RemoveWindow(window);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task CloseAsync_CancellationTokenCancelled()
	{
		var cts = new CancellationTokenSource();

		var app = Application.Current ?? throw new NullReferenceException();

		var page = new ContentPage
		{
			Content = new Label
			{
				Text = "Hello there"
			}
		};

		// Make sure that our page will have a Handler
		CreateViewHandler<MockPageHandler>(page);

		var window = new Window
		{
			Page = page
		};
		app.AddWindow(window);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		// Ensure CancellationToken Has Expired
		await cts.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => popup.CloseAsync(token: cts.Token));
		app.RemoveWindow(window);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task OnOpenedMapperIsCalled()
	{
		var app = Application.Current ?? throw new NullReferenceException();

		var page = new ContentPage
		{
			Content = new Label
			{
				Text = "Hello there"
			}
		};

		// Make sure that our page will have a Handler
		CreateViewHandler<MockPageHandler>(page);

		var window = new Window
		{
			Page = page
		};
		app.AddWindow(window);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		page.ShowPopup(popup);
		Assert.Equal(1, popupHandler.OnOpenedCount);
		await popup.OnDismissedByTappingOutsideOfPopup(TestContext.Current.CancellationToken);

		var popupTask = page.ShowPopupAsync(popup, TestContext.Current.CancellationToken);
		await popup.OnDismissedByTappingOutsideOfPopup(TestContext.Current.CancellationToken);

		await popupTask;

		Assert.Equal(2, popupHandler.OnOpenedCount);
		app.RemoveWindow(window);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task PopupDismissedByTappingOutsideOfPopup()
	{
		var popupClosedTCS = new TaskCompletionSource<(string? Result, bool WasDismissedByTappingOutsideOfPopup)>();
		var app = Application.Current ?? throw new NullReferenceException();

		var page = new ContentPage
		{
			Content = new Label
			{
				Text = "Hello there"
			}
		};

		popup.Closed += (s, e) =>
		{
			Assert.Equal(popup, s);
			popupClosedTCS.SetResult(((string?)e.Result, e.WasDismissedByTappingOutsideOfPopup));
		};

		// Make sure that our page will have a Handler
		CreateViewHandler<MockPageHandler>(page);

		var window = new Window
		{
			Page = page
		};
		app.AddWindow(window);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		await popup.OnDismissedByTappingOutsideOfPopup(TestContext.Current.CancellationToken);

		var (result, wasDismissedByTappingOutsideOfPopup) = await popupClosedTCS.Task;

		Assert.True(wasDismissedByTappingOutsideOfPopup);
		Assert.Equal(resultWhenUserTapsOutsideOfPopup, result);
		app.RemoveWindow(window);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task OnDismissedWithResult()
	{
		object? result = null;
		var isPopupDismissed = false;
		var closedTCS = new TaskCompletionSource();
		var app = Application.Current ?? throw new NullReferenceException();

		var page = new ContentPage
		{
			Content = new Label
			{
				Text = "Hello there"
			}
		};

		// Make sure that our page will have a Handler
		CreateViewHandler<MockPageHandler>(page);

		var window = new Window
		{
			Page = page
		};
		app.AddWindow(window);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		popup.Closed += (_, e) =>
		{
			result = e.Result;
			isPopupDismissed = true;
			closedTCS.SetResult();
		};

		popup.Close(new object());
		await closedTCS.Task;

		Assert.True(isPopupDismissed);
		Assert.NotNull(result);
		app.RemoveWindow(window);
	}


	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task OnDismissedWithoutResult()
	{
		object? result = null;
		var isPopupDismissed = false;
		var app = Application.Current ?? throw new NullReferenceException();

		var page = new ContentPage
		{
			Content = new Label
			{
				Text = "Hello there"
			}
		};

		// Make sure that our page will have a Handler
		CreateViewHandler<MockPageHandler>(page);

		var window = new Window
		{
			Page = page
		};
		app.AddWindow(window);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		popup.Closed += (_, e) =>
		{
			result = e.Result;
			isPopupDismissed = true;
		};

		await popup.CloseAsync(token: TestContext.Current.CancellationToken);

		Assert.True(isPopupDismissed);
		Assert.Null(result);
		app.RemoveWindow(window);
	}

	[Fact]
	public void NullColorThrowsArgumentNullException()
	{
		var popupViewModel = new PopupViewModel();
		var popupWithBinding = new Popup
		{
			BindingContext = popupViewModel
		};
		popupWithBinding.SetBinding(Popup.ColorProperty, nameof(PopupViewModel.Color));

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new Popup { Color = null });
		Assert.Throws<ArgumentNullException>(() => new Popup().Color = null);
		Assert.Throws<ArgumentNullException>(() => popupViewModel.Color = null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopup_IsLogicalChild()
	{
		var app = Application.Current ?? throw new NullReferenceException();

		var page = new ContentPage
		{
			Content = new Label
			{
				Text = "Hello there"
			}
		};

		// Make sure that our page will have a Handler
		CreateViewHandler<MockPageHandler>(page);

		var window = new Window
		{
			Page = page
		};
		app.AddWindow(window);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		Assert.Single(page.LogicalChildrenInternal);
		page.ShowPopup(popup);
		Assert.Equal(2, page.LogicalChildrenInternal.Count);

		await popup.CloseAsync(token: TestContext.Current.CancellationToken);
		Assert.Single(page.LogicalChildrenInternal);
		app.RemoveWindow(window);
	}

	sealed class MockPopup : Popup
	{
		public MockPopup()
		{
			ResultWhenUserTapsOutsideOfPopup = resultWhenUserTapsOutsideOfPopup;
		}
	}

	sealed class PopupViewModel : INotifyPropertyChanged
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

	interface IFooService
	{
		int MyProperty { get; set; }
	}
}