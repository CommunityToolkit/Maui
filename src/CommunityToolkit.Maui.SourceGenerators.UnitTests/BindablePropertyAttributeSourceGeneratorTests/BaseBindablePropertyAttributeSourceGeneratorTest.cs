namespace CommunityToolkit.Maui.SourceGenerators.UnitTests.BindablePropertyAttributeSourceGeneratorTests;

public abstract class BaseBindablePropertyAttributeSourceGeneratorTest : BaseTest
{
	protected static Task VerifySourceGeneratorAsync(string source, params List<(string FileName, string GeneratedFile)> expectedGeneratedFilesList)
		=> VerifySourceGeneratorAsync<BindablePropertyAttributeSourceGenerator>(source, expectedGeneratedFilesList);

	protected static Task VerifySourceGeneratorAsync(string source, string expectedGeneratedFile)
	{
		List<(string FileName, string GeneratedFile)> expectedGeneratedFilesList =
		[
			($"{defaultTestClassName}.g.cs", expectedGeneratedFile)
		];

		return VerifySourceGeneratorAsync<BindablePropertyAttributeSourceGenerator>(source, expectedGeneratedFilesList);
	}
}