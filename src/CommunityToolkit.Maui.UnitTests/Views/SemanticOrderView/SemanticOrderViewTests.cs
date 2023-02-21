using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class SemanticOrderViewTests : BaseHandlerTest
{
	const string resultWhenUserTapsOutsideOfSemanticOrderView = "User Tapped Outside of SemanticOrderView";
	readonly ISemanticOrderView semanticOrderView = new MockSemanticOrderView();

	public SemanticOrderViewTests()
	{
		Assert.IsAssignableFrom<ISemanticOrderView>(new MockSemanticOrderView());
	}

	[Fact]
	public void OnViewOrderIsCalled()
	{
		var semanticOrderViewHandler = CreateViewHandler<MockSemanticOrderViewHandler>(semanticOrderView);
		Assert.NotNull(semanticOrderView.Handler);
		Assert.Equal(1, semanticOrderViewHandler.OnViewOrderCount);
		var button = new Button();
		((MockSemanticOrderView)semanticOrderView).ViewOrder = new[] { button };
		Assert.Single(semanticOrderView.ViewOrder);
		Assert.Equal(button, semanticOrderView.ViewOrder.First());
		Assert.Equal(2, semanticOrderViewHandler.OnViewOrderCount);
	}

	class MockSemanticOrderView : Maui.Views.SemanticOrderView
	{
		public MockSemanticOrderView()
		{

		}
	}
}