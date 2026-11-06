using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;
using CommunityToolkit.Maui.Core.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class SemanticOrderViewTests : BaseViewTest
{
	readonly ISemanticOrderView semanticOrderView = new MockSemanticOrderView();

	public SemanticOrderViewTests()
	{
		Assert.IsType<ISemanticOrderView>(new MockSemanticOrderView(), exactMatch: false);
	}

	[Fact]
	public void HandlerSetsOnVirtualView()
	{
		var semanticOrderViewHandler = CreateViewHandler<MockSemanticOrderViewHandler>(semanticOrderView);
		Assert.NotNull(semanticOrderView.Handler);
		Assert.Equal(semanticOrderViewHandler, semanticOrderView.Handler);
	}

	[Fact]
	public void OnViewOrderIsCalled()
	{
		var semanticOrderViewHandler = CreateViewHandler<MockSemanticOrderViewHandler>(semanticOrderView);
		Assert.NotNull(semanticOrderView.Handler);
		Assert.Equal(1, semanticOrderViewHandler.OnViewOrderCount);
		((MockSemanticOrderView)semanticOrderView).ViewOrder = [new Button()];
		Assert.Equal(2, semanticOrderViewHandler.OnViewOrderCount);
	}

	[Fact]
	public void ISemanticOrderViewViewOrderReturnsCorrectViews()
	{
		CreateViewHandler<MockSemanticOrderViewHandler>(semanticOrderView);

		var button = new Button();
		var entry = new Entry();

		((MockSemanticOrderView)semanticOrderView).ViewOrder = [button, entry];

		Assert.Equal(2, semanticOrderView.ViewOrder.Count());
		Assert.Equal(button, semanticOrderView.ViewOrder.First());
		Assert.Equal(entry, semanticOrderView.ViewOrder.Skip(1).First());
	}

	[Fact]
	public void SetVirtualView_NullView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => new SemanticOrderViewHandler().SetVirtualView(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("view", exception.ParamName);
	}

	class MockSemanticOrderView : Maui.Views.SemanticOrderView
	{
		public MockSemanticOrderView()
		{

		}
	}
}