using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CommunityToolkit.Maui.MediaElement.SourceGenerators.UnitTests;

public abstract class BaseTest
{
	protected const string defaultTestClassName = "TestView";
	protected const string defaultTestNamespace = "TestNamespace";

	protected static async Task VerifySourceGeneratorAsync<TSourceGenerator>(string source, params List<(string FileName, string GeneratedFile)> expectedGeneratedFilesList)
		where TSourceGenerator : IIncrementalGenerator, new()
	{
		const string sourceGeneratorNamespace = "CommunityToolkit.Maui.MediaElement.SourceGenerators";
		var sourceGeneratorFullName = typeof(TSourceGenerator).FullName ?? throw new InvalidOperationException("Source Generator Type Path cannot be null");

		var test = new MediaElementSourceGeneratorTest<TSourceGenerator>
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
					MetadataReference.CreateFromFile(typeof(Microsoft.Maui.Hosting.MauiAppBuilder).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Microsoft.Maui.Controls.BindableObject).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Microsoft.Maui.Controls.BindableProperty).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Microsoft.Maui.Controls.BindingMode).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Microsoft.CodeAnalysis.Accessibility).Assembly.Location),
				}
			}
		};

		// Add minimal placeholders for the CommunityToolkit.Maui.Core namespaces so
		// test sources that include `using CommunityToolkit.Maui.Core` or
		// `using CommunityToolkit.Maui.Core.Views` do not produce missing-namespace
		// diagnostics when the real assembly is not referenced.
		test.TestState.Sources.Add(@"namespace CommunityToolkit.Maui.Core
		{
			// Placeholder type for unit tests
			internal static class __CommunityToolkitMauiCorePlaceholder { }
		}");

		test.TestState.Sources.Add(@"namespace CommunityToolkit.Maui.Core.Views
		{
			// Placeholder type for unit tests
			internal static class __CommunityToolkitMauiCoreViewsPlaceholder { }
		}");

		// Add minimal placeholders for Microsoft.Maui hosting types used in tests so
		// test sources that rely on `MauiApp`, `MauiAppBuilder`, `UseMauiApp` or
		// `Application` do not produce missing-type diagnostics when the real
		// assemblies are not available in the test environment.
		test.TestState.Sources.Add(@"namespace Microsoft.Maui.Hosting
		{
			public class MauiApp
			{
				public static MauiAppBuilder CreateBuilder() => new MauiAppBuilder();
			}

			public class MauiAppBuilder
			{
				public MauiApp Build() => new MauiApp();
			}

			public static class MauiAppBuilderExtensions
			{
				public static MauiAppBuilder UseMauiApp<TApp>(this MauiAppBuilder builder) => builder;
			}
		}");

		// Provide a simple Application placeholder so tests can declare `class App : Application`.
		test.TestState.Sources.Add(@"public class Application { }");

		foreach (var generatedFile in expectedGeneratedFilesList.Where(static x => !string.IsNullOrEmpty(x.GeneratedFile)))
		{
			var expectedGeneratedText = Microsoft.CodeAnalysis.Text.SourceText.From(generatedFile.GeneratedFile, System.Text.Encoding.UTF8);
			var generatedFilePath = Path.Combine(sourceGeneratorNamespace, sourceGeneratorFullName, generatedFile.FileName);
			test.TestState.GeneratedSources.Add((generatedFilePath, expectedGeneratedText));
		}

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	sealed class MediaElementSourceGeneratorTest<TSourceGenerator> : CSharpSourceGeneratorTest<TSourceGenerator, DefaultVerifier>
		where TSourceGenerator : IIncrementalGenerator, new()
	{
		protected override CompilationOptions CreateCompilationOptions()
		{
			var compilationOptions = base.CreateCompilationOptions();
			return compilationOptions;
		}
	}
}

