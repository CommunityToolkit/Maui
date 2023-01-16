using CommunityToolkit.Maui.MediaPlayer.Analyzers;
using Xunit;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public class UseCommunityToolkitMediaPlayerInitializationAnalyzerTests
{
	[Fact]
	public void UseCommunityToolkitMediaPlayerInitializationAnalyzerId()
	{
		Assert.Equal("MCTME001", UseCommunityToolkitMediaPlayerInitializationAnalyzer.DiagnosticId);
	}
}