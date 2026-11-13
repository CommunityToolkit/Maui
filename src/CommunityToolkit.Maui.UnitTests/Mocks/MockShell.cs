namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockShell : Shell
{
	public MockShell(List<ContentPage> shellPages)
	{
		foreach (var page in shellPages)
		{
			Items.Add(page);
		}
	}
}