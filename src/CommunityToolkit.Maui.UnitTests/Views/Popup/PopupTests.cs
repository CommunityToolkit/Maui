using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.Popup;

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

	[Fact]
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

		var popupTask = page.ShowPopupAsync((MockPopup)popup);
		popup.OnDismissedByTappingOutsideOfPopup();

		await popupTask;

		Assert.Equal(2, popupHandler.OnOpenedCount);
	}

	[Fact]
	public void PopupDismissedByTappingOutsideOfPopup()
	{
		string? dismissedByTappingOutsideOfPopupResult = null;
		var isPopupDismissedByTappingOutsideOfPopup = false;
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

			isPopupDismissedByTappingOutsideOfPopup = e.WasDismissedByTappingOutsideOfPopup;
			dismissedByTappingOutsideOfPopupResult = (string?)e.Result;
		};

		// Make sure that our page will have a Handler
		CreateViewHandler<MockPageHandler>(page);

		app.MainPage = page;

		CreateElementHandler<MockPopupHandler>(popup);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		popup.OnDismissedByTappingOutsideOfPopup();

		Assert.True(isPopupDismissedByTappingOutsideOfPopup);
		Assert.Equal(resultWhenUserTapsOutsideOfPopup, dismissedByTappingOutsideOfPopupResult);
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

		((MockPopup)popup).Closed += (_, e) =>
		{
			result = e.Result;
			isPopupDismissed = true;
		};

		((MockPopup)popup).Close(new object());

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

		((MockPopup)popup).Closed += (_, e) =>
		{
			result = e.Result;
			isPopupDismissed = true;
		};

		((MockPopup)popup).Close();

		Assert.True(isPopupDismissed);
		Assert.Null(result);
	}

	[Fact]
	public void NullColorThrowsArgumentNullException()
	{
		var popupViewModel = new PopupViewModel();
		var popupWithBinding = new Maui.Views.Popup
		{
			BindingContext = popupViewModel
		};
		popupWithBinding.SetBinding(Maui.Views.Popup.ColorProperty, nameof(PopupViewModel.Color));

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new Maui.Views.Popup { Color = null });
		Assert.Throws<ArgumentNullException>(() => new Maui.Views.Popup().Color = null);
		Assert.Throws<ArgumentNullException>(() => popupViewModel.Color = null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	class MockPopup : Maui.Views.Popup
	{
		public MockPopup()
		{
			ResultWhenUserTapsOutsideOfPopup = resultWhenUserTapsOutsideOfPopup;
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
				if (value != color)
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