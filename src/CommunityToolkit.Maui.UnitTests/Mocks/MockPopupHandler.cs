using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockPopupHandler : ElementHandler<IPopup, object>
{
	public static CommandMapper<IPopup, MockPopupHandler> PopUpCommandMapper = new(ElementCommandMapper)
	{
		[nameof(IPopup.OnOpened)] = MapOnOpened,
		[nameof(IPopup.OnClosed)] = MapOnClosed,
	};

	public MockPopupHandler() : base(new PropertyMapper<IView>(), PopUpCommandMapper)
	{

	}

	public MockPopupHandler(IPropertyMapper mapper) : base(mapper, PopUpCommandMapper)
	{

	}

	public int OnOpenedCount { get; private set; }

	protected override object CreatePlatformElement()
	{
		return new object();
	}

	static void MapOnOpened(MockPopupHandler arg1, IPopup arg2, object? arg3)
	{
		arg1.OnOpenedCount++;
		arg2.OnOpened();
	}

	static void MapOnClosed(MockPopupHandler handler, IPopup view, object? result)
	{
		view.HandlerCompleteTCS.TrySetResult();
	}
}