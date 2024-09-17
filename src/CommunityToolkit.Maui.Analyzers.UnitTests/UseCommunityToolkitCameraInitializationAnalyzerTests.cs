using CommunityToolkit.Maui.Camera.Analyzers;
using Xunit;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;
public class UseCommunityToolkitCameraInitializationAnalyzerTests
{
	[Fact]
	public void UseCommunityToolkitMediaElementInitializationAnalyzerId()
	{
		Assert.Equal("MCTC001", UseCommunityToolkitCameraInitializationAnalyzer.DiagnosticId);
	}
}
