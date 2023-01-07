using CommunityToolkit.Maui.MediaView.Analyzers;
using Xunit;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public class UseCommunityToolkitMediaViewInitializationAnalyzerTests
{
	[Fact]
	public void UseCommunityToolkitMediaViewInitializationAnalyzerId()
	{
		Assert.Equal("MCTME001", UseCommunityToolkitMediaViewInitializationAnalyzer.DiagnosticId);
	}
}