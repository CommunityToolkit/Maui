using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.Internal.UnitTests;

public abstract class BaseTest
{
	protected static async Task VerifySourceGeneratorAsync(string source, string expectedAttribute, params List<(string FileName, string GeneratedFile)> expectedGenerated)
	{
		const string sourceGeneratorNamespace = "CommunityToolkit.Maui.SourceGenerators.Internal";
		const string bindablePropertyAttributeGeneratedFileName = "BindablePropertyAttribute.g.cs";
		var sourceGeneratorFullName = typeof(BindablePropertyAttributeSourceGenerator).FullName ?? throw new InvalidOperationException("Source Generator Type Path cannot be null");

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


		// Format as UTF 8 to match the default encoding used by Roslyn
		var expectedAttributeText = OperatingSystem.IsWindows() switch
		{
			// Normalize line endings to LF to ensure consistent behavior across platforms, e.g. `<LF>` instead of `<CR><LF>`
			false => Microsoft.CodeAnalysis.Text.SourceText.From(expectedAttribute.Replace("\r\n", "\n").Replace("\r", "\n"), System.Text.Encoding.UTF8),
			true => Microsoft.CodeAnalysis.Text.SourceText.From(expectedAttribute, System.Text.Encoding.UTF8),
		};
		var bindablePropertyAttributeFilePath = Path.Combine(sourceGeneratorNamespace, sourceGeneratorFullName, bindablePropertyAttributeGeneratedFileName);
		test.TestState.GeneratedSources.Add((bindablePropertyAttributeFilePath, expectedAttributeText));

		foreach (var (FileName, GeneratedFile) in expectedGenerated.Where(static x => !string.IsNullOrEmpty(x.GeneratedFile)))
		{
			// Format as UTF 8 to match the default encoding used by Roslyn
			var expectedGeneratedText = OperatingSystem.IsWindows() switch
			{
				// Normalize line endings to LF to ensure consistent behavior across platforms, e.g. `<LF>` instead of `<CR><LF>`
				false => Microsoft.CodeAnalysis.Text.SourceText.From(GeneratedFile.Replace("\r\n", "\n").Replace("\r", "\n"), System.Text.Encoding.UTF8),
				true => Microsoft.CodeAnalysis.Text.SourceText.From(GeneratedFile, System.Text.Encoding.UTF8)
			};

			var generatedFilePath = Path.Combine(sourceGeneratorNamespace, sourceGeneratorFullName, FileName);
			test.TestState.GeneratedSources.Add((generatedFilePath, expectedGeneratedText));
		}

		await test.RunAsync(TestContext.Current.CancellationToken);
	}
}