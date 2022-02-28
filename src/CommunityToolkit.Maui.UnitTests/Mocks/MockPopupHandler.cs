using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockPopupHandler : ElementHandler<IPopup, object>
{
	public static CommandMapper<IPopup, MockPopupHandler> PopUpCommandMapper = new(ElementCommandMapper)
	{
		[nameof(IPopup.OnOpened)] = MapOnOpened
	};

	public MockPopupHandler() : base(new PropertyMapper<IView>(), PopUpCommandMapper)
	{

	}

	public MockPopupHandler(IPropertyMapper mapper) : base(mapper, PopUpCommandMapper)
	{

	}

	public int OnOpenedCount { get; private set; }

	protected override object CreateNativeElement()
	{
		return new object();
	}

	static void MapOnOpened(MockPopupHandler arg1, IPopup arg2, object? arg3)
	{
		arg1.OnOpenedCount++;
	}
}

