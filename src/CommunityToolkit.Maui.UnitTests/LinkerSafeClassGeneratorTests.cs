using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class LinkerSafeClassGeneratorTests : BaseTest
{
	[Fact]
	public void EnsureLinkerSafeClassExists()
	{
		LinkerSafeClass.LinkerSafeMethod();
	}
}

