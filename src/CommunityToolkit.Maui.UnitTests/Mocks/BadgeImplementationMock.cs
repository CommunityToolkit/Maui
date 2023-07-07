using CommunityToolkit.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class BadgeImplementationMock : IBadge
{
	public int GetCount()
	{
		throw new NotImplementedException();
	}

	public void SetCount(int count)
	{
		throw new NotImplementedException();
	}
}