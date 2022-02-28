using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupTests : BaseHandlerTest
{
	readonly IPopup popup = new MockPopup();

	public PopupTests()
	{
		Assert.IsAssignableFrom<IPopup>(new MockPopup());
		Assert.IsAssignableFrom<IPopup>(new MockBasePopup());
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

	[Fact]
	public async Task OnOnpenedMapperIsCalled()
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
		popup.LightDismiss();

		var popupTask = page.ShowPopupAsync((MockPopup)popup);
		popup.LightDismiss();

		await popupTask;

		Assert.Equal(2, popupHandler.OnOpenedCount);
	}

	[Fact]
	public void OnLightDismissedHappens()
	{
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

		CreateElementHandler<MockPopupHandler>(popup);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		((MockPopup)popup).Dismissed += (_, _) =>
		{
			isPopupDismissed = true;
		};

		popup.LightDismiss();
		Assert.True(isPopupDismissed);
	}

	[Fact]
	public void OnDismissedWithResult()
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

		((MockPopup)popup).Dismissed += (_, e) =>
		{
			result = e.Result;
			isPopupDismissed = true;
		};

		((MockPopup)popup).Dismiss(new object());

		Assert.True(isPopupDismissed);
		Assert.NotNull(result);
	}


	[Fact]
	public void OnDismissedWithoutResult()
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

		((MockPopup)popup).Dismissed += (_, e) =>
		{
			result = e.Result;
			isPopupDismissed = true;
		};

		((MockPopup)popup).Dismiss(null);

		Assert.True(isPopupDismissed);
		Assert.Null(result);
	}

	class MockPopup : Popup
	{

	}

	class MockBasePopup : BasePopup
	{

	}

	interface IFooService
	{
		public int MyProperty { get; set; }
	}
}

