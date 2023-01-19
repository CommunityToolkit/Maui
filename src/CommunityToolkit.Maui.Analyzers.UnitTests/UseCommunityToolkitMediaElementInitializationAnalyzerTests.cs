using CommunityToolkit.Maui.MediaElement.Analyzers;
using Xunit;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public class UseCommunityToolkitMediaElementInitializationAnalyzerTests
{
	[Fact]
	public void UseCommunityToolkitMediaElementInitializationAnalyzerId()
	{
		Assert.Equal("MCTME001", UseCommunityToolkitMediaElementInitializationAnalyzer.DiagnosticId);
	}
}