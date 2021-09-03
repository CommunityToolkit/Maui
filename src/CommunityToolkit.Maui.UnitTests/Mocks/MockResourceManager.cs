using System.Globalization;
using System.Resources;

namespace CommunityToolkit.Maui.UnitTests.Mocks
{
	class MockResourceManager : ResourceManager
	{
		public override string GetString(string name, CultureInfo? culture) => culture?.EnglishName ?? string.Empty;
	}
}