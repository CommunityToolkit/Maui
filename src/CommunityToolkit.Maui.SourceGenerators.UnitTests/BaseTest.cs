using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public abstract class BaseTest
{
	protected const string defaultTestClassName = "TestView";
	protected const string defaultTestNamespace = "TestNamespace";

	protected static async Task VerifySourceGeneratorAsync<TSourceGenerator>(string source, params List<(string FileName, string GeneratedFile)> expectedGeneratedFilesList)
		where TSourceGenerator : IIncrementalGenerator, new()
	{
		const string sourceGeneratorNamespace = "CommunityToolkit.Maui.SourceGenerators";
		var sourceGeneratorFullName = typeof(TSourceGenerator).FullName ?? throw new InvalidOperationException("Source Generator Type Path cannot be null");

		var test = new ExperimentalBindablePropertyTest<TSourceGenerator>
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
					MetadataReference.CreateFromFile(typeof(Microsoft.Maui.Controls.BindingMode).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Microsoft.CodeAnalysis.Accessibility).Assembly.Location),
				}
			}
		};

		foreach (var generatedFile in expectedGeneratedFilesList.Where(static x => !string.IsNullOrEmpty(x.GeneratedFile)))
		{
			var expectedGeneratedText = Microsoft.CodeAnalysis.Text.SourceText.From(generatedFile.GeneratedFile, System.Text.Encoding.UTF8);
			var generatedFilePath = Path.Combine(sourceGeneratorNamespace, sourceGeneratorFullName, generatedFile.FileName);
			test.TestState.GeneratedSources.Add((generatedFilePath, expectedGeneratedText));
		}

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	// This class can be deleted once [Experimental] is removed from BindablePropertyAttribute
	sealed class ExperimentalBindablePropertyTest<TSourceGenerator> : CSharpSourceGeneratorTest<TSourceGenerator, DefaultVerifier>
		where TSourceGenerator : IIncrementalGenerator, new()
	{
		protected override CompilationOptions CreateCompilationOptions()
		{
			var compilationOptions = base.CreateCompilationOptions();

			return compilationOptions.WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
			{
				{ BindablePropertyDiagnostic.BindablePropertyAttributeExperimentalDiagnosticId, ReportDiagnostic.Warn }
			});
		}
	}
}