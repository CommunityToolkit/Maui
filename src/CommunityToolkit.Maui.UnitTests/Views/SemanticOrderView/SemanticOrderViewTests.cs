using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

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

	class MockSemanticOrderView : Maui.Views.SemanticOrderView
	{
		public MockSemanticOrderView()
		{

		}
	}
}