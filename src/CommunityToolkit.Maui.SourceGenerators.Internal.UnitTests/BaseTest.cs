using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.Internal.UnitTests;

public abstract class BaseTest
{
	protected static async Task VerifySourceGeneratorAsync(string source, string expectedAttribute, params List<(string FileName,string GeneratedFile)> expectedGenerated)
	{
		var test = new CSharpSourceGeneratorTest<BindablePropertyAttributeSourceGenerator, DefaultVerifier>
		{
#if NET10_0
			ReferenceAssemblies = Microsoft.CodeAnalysis.Testing.ReferenceAssemblies.Net.Net100,
#else
#error ReferenceAssemblies must be updated to current version of .NET
#endif
			TestState =
			{
				Sources = { source },

				AdditionalReferences =
				{
					MetadataReference.CreateFromFile(typeof(Microsoft.Maui.Controls.BindableObject).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Microsoft.Maui.Controls.BindableProperty).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Microsoft.Maui.Controls.BindingMode).Assembly.Location)
				}
			}
		};
		var expectedAttributeText = Microsoft.CodeAnalysis.Text.SourceText.From(expectedAttribute, System.Text.Encoding.UTF8);
		test.TestState.GeneratedSources.Add(("CommunityToolkit.Maui.SourceGenerators.Internal\\CommunityToolkit.Maui.SourceGenerators.Internal.BindablePropertyAttributeSourceGenerator\\BindablePropertyAttribute.g.cs", expectedAttributeText));


		foreach (var generatedFile in expectedGenerated)
		{
			if (!string.IsNullOrEmpty(generatedFile.GeneratedFile))
			{
				var expectedGeneratedText = Microsoft.CodeAnalysis.Text.SourceText.From(generatedFile.GeneratedFile, System.Text.Encoding.UTF8);
				test.TestState.GeneratedSources.Add(($"CommunityToolkit.Maui.SourceGenerators.Internal\\CommunityToolkit.Maui.SourceGenerators.Internal.BindablePropertyAttributeSourceGenerator\\{generatedFile.FileName}", expectedGeneratedText));
			}
		}

		await test.RunAsync(TestContext.Current.CancellationToken);
	}
}