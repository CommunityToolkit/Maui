namespace CommunityToolkit.Maui.SourceGenerators.UnitTests.AttachedBindablePropertyAttributeSourceGeneratorTests;

public abstract class BaseAttachedBindablePropertyAttributeSourceGeneratorTest : BaseTest
{
	protected static Task VerifySourceGeneratorAsync(string source, params List<(string FileName, string GeneratedFile)> expectedGeneratedFilesList)
		=> VerifySourceGeneratorAsync<AttachedBindablePropertyAttributeSourceGenerator>(source, expectedGeneratedFilesList);


	protected static Task VerifySourceGeneratorAsync(string source, string expectedGeneratedFile)
	{
		List<(string FileName, string GeneratedFile)> expectedGeneratedFilesList =
		[
			($"{defaultTestClassName}.g.cs", expectedGeneratedFile)
		];

		return VerifySourceGeneratorAsync<AttachedBindablePropertyAttributeSourceGenerator>(source, expectedGeneratedFilesList);
	}
}