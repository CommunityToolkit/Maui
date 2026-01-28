using CommunityToolkit.Maui.MediaElement.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CommunityToolkit.Maui.MediaElement.SourceGenerators.UnitTests.AndroidMediaElementServiceConfigurationAnalyzerTests;

public class AndroidMediaElementServiceConfigurationAnalyzer_EdgeCaseTests : BaseAndroidMediaElementServiceConfigurationAnalyzerTest
{
	[Fact]
	public async Task Analyzer_WhenClassNameContainsMediaElementOptions_ReportsDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using CommunityToolkit.Maui.Core;

			namespace {{defaultTestNamespace}};

			public class CustomMediaElementOptionsExtended
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
	public async Task Analyzer_WhenPropertyInitializedWithComplexExpression_ReportsNoDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using CommunityToolkit.Maui.Core;

			namespace {{defaultTestNamespace}};

			public class MediaElementOptions
			{
				internal static bool IsAndroidForegroundServiceEnabled { get; private set; } = GetDefaultValue();

				static bool GetDefaultValue() => true;
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task Analyzer_WhenInternalClass_ReportsDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using CommunityToolkit.Maui.Core;

			namespace {{defaultTestNamespace}};

			internal class MediaElementOptions
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
	public async Task Analyzer_WhenMethodInvokedMultipleTimes_ReportsMultipleDiagnostics()
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
					options.SetIsAndroidForegroundServiceEnabled({|#1:true|});
				}
			}
			""";

		var expectedDiagnostic1 = new DiagnosticResult(AndroidMediaElementForegroundServiceConfigurationAnalyzer.Rule)
			.WithLocation(0)
			.WithSeverity(DiagnosticSeverity.Error);

		var expectedDiagnostic2 = new DiagnosticResult(AndroidMediaElementForegroundServiceConfigurationAnalyzer.Rule)
			.WithLocation(1)
			.WithSeverity(DiagnosticSeverity.Error);

		await VerifyAnalyzerAsync(source, expectedDiagnostic1, expectedDiagnostic2);
	}

	[Fact]
	public async Task Analyzer_WhenPartialClass_ReportsDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using CommunityToolkit.Maui.Core;

			namespace {{defaultTestNamespace}};

			public partial class MediaElementOptions
			{
				internal static bool {|#0:IsAndroidForegroundServiceEnabled|} { get; private set; } = true;
			}

			public partial class MediaElementOptions
			{
				public void OtherMethod() { }
			}
			""";

		var expectedDiagnostic = new DiagnosticResult(AndroidMediaElementForegroundServiceConfigurationAnalyzer.Rule)
			.WithLocation(0)
			.WithSeverity(DiagnosticSeverity.Error);

		await VerifyAnalyzerAsync(source, expectedDiagnostic);
	}

	[Fact]
	public async Task Analyzer_WhenMethodNotOnMediaElementOptions_ReportsNoDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using CommunityToolkit.Maui.Core;

			namespace {{defaultTestNamespace}};

			public class OtherOptions
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
					var options = new OtherOptions();
					options.SetIsAndroidForegroundServiceEnabled(true);
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task Analyzer_WhenPropertyIsNotStatic_ReportsNoDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using CommunityToolkit.Maui.Core;

			namespace {{defaultTestNamespace}};

			public class MediaElementOptions
			{
				internal bool IsAndroidForegroundServiceEnabled { get; private set; } = true;
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task Analyzer_WhenPropertyIsPublic_ReportsDiagnostic()
	{
		const string source =
			/* language=C#-test */
			$$"""
			using CommunityToolkit.Maui.Core;

			namespace {{defaultTestNamespace}};

			public class MediaElementOptions
			{
				public static bool {|#0:IsAndroidForegroundServiceEnabled|} { get; private set; } = true;
			}
			""";

		var expectedDiagnostic = new DiagnosticResult(AndroidMediaElementForegroundServiceConfigurationAnalyzer.Rule)
			.WithLocation(0)
			.WithSeverity(DiagnosticSeverity.Error);

		await VerifyAnalyzerAsync(source, expectedDiagnostic);
	}
}
