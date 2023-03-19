using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.LazyView;
public class LazyViewTests : BaseHandlerTest
{
	[Fact]
	public void CheckIsLoadedFalseAfterConstruction()
	{
		var lazyView = new LazyView<Button>();
		Assert.False(lazyView.IsLoaded);
	}

	[Fact]
	public void CheckIsLoadedTrueAfterCallLoadViewAsync()
	{
		var lazyView = new LazyView<Button>();
		lazyView.LoadViewAsync();
		Assert.True(lazyView.IsLoaded);
	}

	[Fact]
	public void CheckContentSetAfterLoadViewAsync()
	{
		var lazyView = new LazyView<Button>();
		lazyView.LoadViewAsync();
		Assert.True(lazyView.IsLoaded);
		Assert.True(lazyView.Content is Button);
	}

	[Fact]
	public void CheckBindingContextIsPassedToCreatedView()
	{
		var lazyView = new LazyView<Button>();
		var bindingContext = new object();
		lazyView.BindingContext = bindingContext;
		lazyView.LoadViewAsync();
		Assert.True(lazyView.IsLoaded);
		Assert.True(lazyView.Content is Button);
		Assert.Equal(bindingContext, lazyView.Content.BindingContext);
	}
}
