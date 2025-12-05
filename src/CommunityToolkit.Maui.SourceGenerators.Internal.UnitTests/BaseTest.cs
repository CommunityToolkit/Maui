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

		var test = new ExperimentalBindablePropertyTest
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
		var bindablePropertyAttributeFilePath = Path.Combine(sourceGeneratorNamespace, sourceGeneratorFullName, bindablePropertyAttributeGeneratedFileName);
		test.TestState.GeneratedSources.Add((bindablePropertyAttributeFilePath, expectedAttributeText));

		foreach (var generatedFile in expectedGenerated.Where(static x => !string.IsNullOrEmpty(x.GeneratedFile)))
		{
			var expectedGeneratedText = Microsoft.CodeAnalysis.Text.SourceText.From(generatedFile.GeneratedFile, System.Text.Encoding.UTF8);
			var generatedFilePath = Path.Combine(sourceGeneratorNamespace, sourceGeneratorFullName, generatedFile.FileName);
			test.TestState.GeneratedSources.Add((generatedFilePath, expectedGeneratedText));
		}

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	// This class can be deleted once [Experimental] is removed from BindablePropertyAttribute
	sealed class ExperimentalBindablePropertyTest : CSharpSourceGeneratorTest<BindablePropertyAttributeSourceGenerator, DefaultVerifier>
	{
		protected override CompilationOptions CreateCompilationOptions()
		{
			var compilationOptions = base.CreateCompilationOptions();

			return compilationOptions.WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
			{
				{ BindablePropertyAttributeSourceGenerator.BindablePropertyAttributeExperimentalDiagnosticId, ReportDiagnostic.Warn }
			});
		}
	}
}