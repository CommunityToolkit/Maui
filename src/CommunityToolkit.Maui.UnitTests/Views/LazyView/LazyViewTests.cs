using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class LazyViewTests : BaseHandlerTest
{
	[Fact]
	public void CheckHasLoadedFalseAfterConstruction()
	{
		var lazyView = new LazyView<Button>();

		Assert.False(lazyView.HasLazyViewLoaded);
	}

	[Fact]
	public async Task CheckHasLoadedTrueAfterCallLoadView()
	{
		var lazyView = new LazyView<Button>();

		await lazyView.LoadViewAsync();

		Assert.True(lazyView.HasLazyViewLoaded);
	}

	[Fact]
	public async Task CheckContentSetAfterLoadView()
	{
		var lazyView = new LazyView<Button>();

		await lazyView.LoadViewAsync();

		Assert.True(lazyView.HasLazyViewLoaded);
		Assert.IsAssignableFrom<Button>(lazyView.Content);
	}

	[Fact]
	public async Task CheckBindingContextIsPassedToCreatedView()
	{
		var lazyView = new LazyView<Button>();
		var bindingContext = new object();
		lazyView.BindingContext = bindingContext;

		await lazyView.LoadViewAsync();

		Assert.True(lazyView.HasLazyViewLoaded);
		Assert.IsAssignableFrom<Button>(lazyView.Content);
		Assert.Equal(bindingContext, lazyView.Content.BindingContext);
	}
}