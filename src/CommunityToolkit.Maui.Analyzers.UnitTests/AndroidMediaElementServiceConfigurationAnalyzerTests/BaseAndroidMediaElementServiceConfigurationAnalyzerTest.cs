using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis;
using Xunit;
using CommunityToolkit.Maui.MediaElement.Analyzers;

namespace CommunityToolkit.Maui.MediaElement.SourceGenerators.UnitTests.AndroidMediaElementServiceConfigurationAnalyzerTests;

public abstract class BaseAndroidMediaElementServiceConfigurationAnalyzerTest
{
	protected const string defaultTestClassName = "TestView";
	protected const string defaultTestNamespace = "TestNamespace";

	protected static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expectedDiagnostics)
	{
		var test = new CSharpAnalyzerTest<AndroidMediaElementForegroundServiceConfigurationAnalyzer, DefaultVerifier>
		{
#if NET10_0
			ReferenceAssemblies = Microsoft.CodeAnalysis.Testing.ReferenceAssemblies.Net.Net100,
#else
#error ReferenceAssemblies must be updated to current version of .NET
#endif
			TestState =
			{
				Sources = 
				{
					source,
					// Add minimal placeholders for the CommunityToolkit.Maui.Core namespaces so
					// test sources that include `using CommunityToolkit.Maui.Core` do not produce
					// missing-namespace diagnostics when the real assembly is not referenced.
					@"namespace CommunityToolkit.Maui.Core
		{
			// Placeholder type for unit tests
			internal static class __CommunityToolkitMauiCorePlaceholder { }
		}"
				},
				AdditionalReferences =
				{
					MetadataReference.CreateFromFile(typeof(Microsoft.Maui.Hosting.MauiAppBuilder).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(Microsoft.Maui.Controls.BindableObject).Assembly.Location),
				}
			}
		};

		test.TestState.ExpectedDiagnostics.AddRange(expectedDiagnostics);

		await test.RunAsync(TestContext.Current.CancellationToken);
	}
}
