using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockApplication : Application
{
	public new Application? Current = null;
}