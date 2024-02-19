using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupTests : BaseHandlerTest
{
	const string resultWhenUserTapsOutsideOfPopup = "User Tapped Outside of Popup";
	readonly IPopup popup = new MockPopup();

	public PopupTests()
	{
		Assert.IsAssignableFrom<IPopup>(new MockPopup());
	}

	[Fact]
	public void GetRequiredServiceThrowsOnNoContext()
	{
		var handlerStub = new MockPopupHandler();

		Assert.Null((handlerStub as IElementHandler).MauiContext);

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

		app.MainPage = page;

		var popupHandler = CreateElementHandler<MockPopupHandler>(popup);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		// Ensure CancellationToken Has Expired
		await Task.Delay(100, CancellationToken.None);

		await Assert.ThrowsAsync<TaskCanceledException>(() => page.ShowPopupAsync((MockPopup)popup, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenCancelled()
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

		app.MainPage = page;

		var popupHandler = CreateElementHandler<MockPopupHandler>(popup);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		// Ensure CancellationToken Has Expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<TaskCanceledException>(() => page.ShowPopupAsync((MockPopup)popup, cts.Token));
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

		app.MainPage = page;

		var popupHandler = CreateElementHandler<MockPopupHandler>(popup);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		// Ensure CancellationToken Has Expired
		await Task.Delay(100, CancellationToken.None);

		await Assert.ThrowsAsync<TaskCanceledException>(() => ((MockPopup)popup).CloseAsync(token: cts.Token));
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

		app.MainPage = page;

		var popupHandler = CreateElementHandler<MockPopupHandler>(popup);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		// Ensure CancellationToken Has Expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<TaskCanceledException>(() => ((MockPopup)popup).CloseAsync(token: cts.Token));
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

		app.MainPage = page;

		var popupHandler = CreateElementHandler<MockPopupHandler>(popup);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		page.ShowPopup((MockPopup)popup);
		Assert.Equal(1, popupHandler.OnOpenedCount);
		popup.OnDismissedByTappingOutsideOfPopup();

		var popupTask = page.ShowPopupAsync((MockPopup)popup, CancellationToken.None);
		popup.OnDismissedByTappingOutsideOfPopup();

		await popupTask;

		Assert.Equal(2, popupHandler.OnOpenedCount);
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

		((MockPopup)popup).Closed += (s, e) =>
		{
			Assert.Equal(popup, s);
			popupClosedTCS.SetResult(((string?)e.Result, e.WasDismissedByTappingOutsideOfPopup));
		};

		// Make sure that our page will have a Handler
		CreateViewHandler<MockPageHandler>(page);

		app.MainPage = page;

		CreateElementHandler<MockPopupHandler>(popup);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		popup.OnDismissedByTappingOutsideOfPopup();

		var (result, wasDismissedByTappingOutsideOfPopup) = await popupClosedTCS.Task;

		Assert.True(wasDismissedByTappingOutsideOfPopup);
		Assert.Equal(resultWhenUserTapsOutsideOfPopup, result);
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

		app.MainPage = page;

		// Make sure that our popup will have a Handler
		CreateElementHandler<MockPopupHandler>(popup);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		((MockPopup)popup).Closed += (_, e) =>
		{
			result = e.Result;
			isPopupDismissed = true;
			closedTCS.TrySetResult();
		};

		((MockPopup)popup).Close(new object());
		await closedTCS.Task;

		Assert.True(isPopupDismissed);
		Assert.NotNull(result);
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

		app.MainPage = page;

		// Make sure that our popup will have a Handler
		CreateElementHandler<MockPopupHandler>(popup);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		((MockPopup)popup).Closed += (_, e) =>
		{
			result = e.Result;
			isPopupDismissed = true;
		};

		await ((MockPopup)popup).CloseAsync(token: CancellationToken.None);

		Assert.True(isPopupDismissed);
		Assert.Null(result);
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

		app.MainPage = page;

		// Make sure that our popup will have a Handler
		CreateElementHandler<MockPopupHandler>(popup);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		Assert.Single(page.LogicalChildrenInternal);
		page.ShowPopup((MockPopup)popup);
		Assert.Equal(2, page.LogicalChildrenInternal.Count);

		await((MockPopup)popup).CloseAsync(token: CancellationToken.None);
		Assert.Single(page.LogicalChildrenInternal);
	}

	class MockPopup : Popup
	{
		public MockPopup()
		{
			ResultWhenUserTapsOutsideOfPopup = resultWhenUserTapsOutsideOfPopup;
		}

		protected override async Task OnClosed(object? result, bool wasDismissedByTappingOutsideOfPopup, CancellationToken token)
		{
			await Task.Delay(100, token);

			((IPopup)this).HandlerCompleteTCS.TrySetResult();

			await base.OnClosed(result, wasDismissedByTappingOutsideOfPopup, token);
		}
	}

	class PopupViewModel : INotifyPropertyChanged
	{
		Color? color = new();

		public event PropertyChangedEventHandler? PropertyChanged;

		public Color? Color
		{
			get => color;
			set
			{
				if (!Equals(value, color))
				{
					color = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
				}
			}
		}
	}

	interface IFooService
	{
		public int MyProperty { get; set; }
	}
}