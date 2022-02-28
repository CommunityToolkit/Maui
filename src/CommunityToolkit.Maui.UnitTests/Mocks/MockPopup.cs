using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockPopupHandler : ElementHandler<IPopup, object>
{
	public int OnOpenedCount { get; set; }
	public static CommandMapper<IPopup, MockPopupHandler> PopUpCommandMapper = new(ElementCommandMapper)
	{
		[nameof(IPopup.OnOpened)] = MapOnOpened
	};

	static void MapOnOpened(MockPopupHandler arg1, IPopup arg2, object? arg3)
	{
		arg1.OnOpenedCount++;
	}

	public MockPopupHandler() : base(new PropertyMapper<IView>(), PopUpCommandMapper)
	{

	}


	public MockPopupHandler(IPropertyMapper mapper) : base(mapper, PopUpCommandMapper)
	{

	}

	protected override object CreateNativeElement()
	{
		return new object();
	}
}

