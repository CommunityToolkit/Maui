using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockSemanticOrderViewHandler : ViewHandler<ISemanticOrderView, object>
{
	public static PropertyMapper<ISemanticOrderView, MockSemanticOrderViewHandler> SemanticOrderViewPropertyMapper = new()
	{
		[nameof(ISemanticOrderView.ViewOrder)] = MapViewOrder
	};

	public MockSemanticOrderViewHandler() : base(SemanticOrderViewPropertyMapper)
	{

	}

	public int OnViewOrderCount { get; private set; }

	protected override object CreatePlatformView()
	{
		return new object();
	}

	static void MapViewOrder(MockSemanticOrderViewHandler handler, ISemanticOrderView view)
	{
		handler.OnViewOrderCount++;
	}
}