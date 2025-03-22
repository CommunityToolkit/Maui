using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockPageViewModel : BindableObject
{
	public bool HasLoaded { get; set; }
}