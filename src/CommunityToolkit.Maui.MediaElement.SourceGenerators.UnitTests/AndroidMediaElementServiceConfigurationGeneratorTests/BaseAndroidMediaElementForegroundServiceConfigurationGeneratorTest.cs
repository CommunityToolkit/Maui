namespace CommunityToolkit.Maui.MediaElement.SourceGenerators.UnitTests.AndroidMediaElementServiceConfigurationGeneratorTests;

public abstract class BaseAndroidMediaElementForegroundServiceConfigurationGeneratorTest : BaseTest
{
	protected static Task VerifySourceGeneratorAsync(string source, string expectedGeneratedFile)
	{
		List<(string FileName, string GeneratedFile)> expectedGeneratedFilesList =
		[
			("AndroidMediaElementServiceConfiguration.g.cs", expectedGeneratedFile)
		];

		return VerifySourceGeneratorAsync<AndroidMediaElementForegroundServiceConfigurationGenerator>(source, expectedGeneratedFilesList);
	}

	protected static Task VerifySourceGeneratorAsync(string source)
	{
		List<(string FileName, string GeneratedFile)> expectedGeneratedFilesList = [];
		return VerifySourceGeneratorAsync<AndroidMediaElementForegroundServiceConfigurationGenerator>(source, expectedGeneratedFilesList);
	}
}