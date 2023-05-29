using CommunityToolkit.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class BadgeImplementationMock : IBadge
{
	public void SetCount(int count)
	{
		throw new NotImplementedException();
	}
}