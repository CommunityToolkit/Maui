using BenchmarkDotNet.Attributes;
using CommunityToolkit.Maui.MediaElement.SourceGenerators.UnitTests.AndroidMediaElementServiceConfigurationAnalyzerTests;

namespace CommunityToolkit.Maui.Analyzers.Benchmarks;

class AndroidMediaElementServiceConfigurationAnalyzerBenchmarks
{
	static readonly AndroidMediaElementServiceConfigurationAnalyzer_CommonUsageTests androidMediaElementServiceConfigurationAnalyzer_CommonUsageTests = new();

	[Benchmark]
	public Task WhenNoMediaElementOptions_ReportsNoDiagnostic()
	{
		return androidMediaElementServiceConfigurationAnalyzer_CommonUsageTests.Analyzer_WhenNoMediaElementOptions_ReportsNoDiagnostic();
	}

	[Benchmark]
	public Task WhenMethodInvokedWithTrue_ReportsDiagnostic()
	{
		return androidMediaElementServiceConfigurationAnalyzer_CommonUsageTests.Analyzer_WhenMethodInvokedWithTrue_ReportsDiagnostic();
	}

	[Benchmark]
	public Task WhenMultiplePropertiesSetToTrue_ReportsSingleDiagnostic()
	{
		return androidMediaElementServiceConfigurationAnalyzer_CommonUsageTests.Analyzer_WhenMultiplePropertiesSetToTrue_ReportsSingleDiagnostic();
	}
}
