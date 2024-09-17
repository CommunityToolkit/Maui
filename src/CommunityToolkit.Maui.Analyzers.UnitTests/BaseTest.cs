namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public class BaseTest
{
	protected const string appCS = @"
namespace CommunityToolkit.Maui.Analyzers.UnitTests
{
	public partial class App : Microsoft.Maui.Controls.Application
	{
		public App()
		{
		}
	}
}";
}
