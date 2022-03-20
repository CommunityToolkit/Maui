using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockPageHandler : ViewHandler<IContentView, object>
{
	public MockPageHandler() : base(new PropertyMapper<IView>())
	{

	}


	public MockPageHandler(IPropertyMapper mapper) : base(mapper)
	{

	}

	protected override object CreatePlatformView()
	{
		return new object();
	}
}