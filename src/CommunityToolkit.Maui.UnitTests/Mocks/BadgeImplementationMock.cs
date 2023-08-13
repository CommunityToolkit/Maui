using CommunityToolkit.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class BadgeImplementationMock : IBadge
{
	public void SetCount(uint count)
	{
		throw new NotImplementedException();
	}
}