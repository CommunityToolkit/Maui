using CommunityToolkit.Maui.MediaElement.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CommunityToolkit.Maui.MediaElement.SourceGenerators.UnitTests.AndroidMediaElementServiceConfigurationAnalyzerTests;

public class AndroidMediaElementForegroundServiceConfigurationAnalyzer_CommonUsageTests : BaseAndroidMediaElementServiceConfigurationAnalyzerTest
{
	[Fact]
	public async Task Analyzer_WhenPropertySetToTrue_ReportsDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using CommunityToolkit.Maui.Core;

			namespace {{defaultTestNamespace}};

			public class MediaElementOptions
			{
				internal static bool {|#0:IsAndroidForegroundServiceEnabled|} { get; private set; } = true;
			}
			""";

		var expectedDiagnostic = new DiagnosticResult(AndroidMediaElementForegroundServiceConfigurationAnalyzer.Rule)
			.WithLocation(0)
			.WithSeverity(DiagnosticSeverity.Error);

		await VerifyAnalyzerAsync(source, expectedDiagnostic);
	}

	[Fact]
	public async Task Analyzer_WhenMethodInvokedWithTrue_ReportsDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using CommunityToolkit.Maui.Core;

			namespace {{defaultTestNamespace}};

			public class MediaElementOptions
			{
				internal static bool IsAndroidForegroundServiceEnabled { get; private set; } = false;

				public void SetIsAndroidForegroundServiceEnabled(bool isEnabled)
				{
					IsAndroidForegroundServiceEnabled = isEnabled;
				}
			}

			public class TestConfiguration
			{
				public void Configure()
				{
					var options = new MediaElementOptions();
					options.SetIsAndroidForegroundServiceEnabled({|#0:true|});
				}
			}
			""";

		var expectedDiagnostic = new DiagnosticResult(AndroidMediaElementForegroundServiceConfigurationAnalyzer.Rule)
			.WithLocation(0)
			.WithSeverity(DiagnosticSeverity.Error);

		await VerifyAnalyzerAsync(source, expectedDiagnostic);
	}

	[Fact]
	public async Task Analyzer_WhenPropertySetToFalse_ReportsNoDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using CommunityToolkit.Maui.Core;

			namespace {{defaultTestNamespace}};

			public class MediaElementOptions
			{
				internal static bool IsAndroidForegroundServiceEnabled { get; private set; } = false;
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task Analyzer_WhenMethodInvokedWithFalse_ReportsNoDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using CommunityToolkit.Maui.Core;

			namespace {{defaultTestNamespace}};

			public class MediaElementOptions
			{
				internal static bool IsAndroidForegroundServiceEnabled { get; private set; } = false;

				public void SetIsAndroidForegroundServiceEnabled(bool isEnabled)
				{
					IsAndroidForegroundServiceEnabled = isEnabled;
				}
			}

			public class TestConfiguration
			{
				public void Configure()
				{
					var options = new MediaElementOptions();
					options.SetIsAndroidForegroundServiceEnabled(false);
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task Analyzer_WhenNoMediaElementOptions_ReportsNoDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using Microsoft.Maui.Controls;

			namespace {{defaultTestNamespace}};

			public class {{defaultTestClassName}} : View
			{
				public string Text { get; set; } = "";
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task Analyzer_WhenPropertyHasNoInitializer_ReportsNoDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using CommunityToolkit.Maui.Core;

			namespace {{defaultTestNamespace}};

			public class MediaElementOptions
			{
				internal static bool IsAndroidForegroundServiceEnabled { get; private set; }
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task Analyzer_WhenMethodInvokedWithVariable_ReportsNoDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using CommunityToolkit.Maui.Core;

			namespace {{defaultTestNamespace}};

			public class MediaElementOptions
			{
				internal static bool IsAndroidForegroundServiceEnabled { get; private set; } = false;

				public void SetIsAndroidForegroundServiceEnabled(bool isEnabled)
				{
					IsAndroidForegroundServiceEnabled = isEnabled;
				}
			}

			public class TestConfiguration
			{
				public void Configure()
				{
					var options = new MediaElementOptions();
					bool enableService = true;
					options.SetIsAndroidForegroundServiceEnabled(enableService);
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task Analyzer_WhenMultiplePropertiesSetToTrue_ReportsSingleDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using CommunityToolkit.Maui.Core;

			namespace {{defaultTestNamespace}};

			public class MediaElementOptions
			{
				internal static bool {|#0:IsAndroidForegroundServiceEnabled|} { get; private set; } = true;
				internal static bool AnotherProperty { get; private set; } = true;
			}
			""";

		var expectedDiagnostic = new DiagnosticResult(AndroidMediaElementForegroundServiceConfigurationAnalyzer.Rule)
			.WithLocation(0)
			.WithSeverity(DiagnosticSeverity.Error);

		await VerifyAnalyzerAsync(source, expectedDiagnostic);
	}
}
