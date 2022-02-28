using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockApplication : Application
{
	public MockApplication()
	{

	}

	public new Application? Current = null;
}

//https://github.com/dotnet/maui/blob/main/src/Controls/tests/Core.UnitTests/TestClasses/ApplicationHandlerStub.cs
class ApplicationHandlerStub : ElementHandler<IApplication, object>
{
	public static IPropertyMapper<IApplication, ApplicationHandlerStub> Mapper = new PropertyMapper<IApplication, ApplicationHandlerStub>(ElementMapper)
	{
	};
	public ApplicationHandlerStub()
		: base(Mapper)
	{
	}

	protected override object CreateNativeElement()
	{
		return new object();
	}
}