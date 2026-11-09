using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockPageViewModel : BindableObject
{
	public bool HasLoaded { get; set; }
}

public class LongLivedMockPageViewModel : MockPageViewModel
{
}

public class ShortLivedMockPageViewModel : MockPageViewModel
{
}