using BenchmarkDotNet.Attributes;
using CommunityToolkit.Maui.Analyzers.UnitTests;

namespace CommunityToolkit.Maui.Analyzers.Benchmarks;

[MemoryDiagnoser]
public class CommunityToolkitMauiAnalyzerBenchmarks
{
	static readonly UseCommunityToolkitInitializationAnalyzerTests useCommunityToolkitInitializationAnalyzerTests = new();

	[Benchmark]
	public Task VerifyNoErrorsWhenUseMauiCommunityToolkit()
	{
		return useCommunityToolkitInitializationAnalyzerTests.VerifyNoErrorsWhenUseMauiCommunityToolkit();
	}

	[Benchmark]
	public Task VerifyNoErrorsWhenUseMauiCommunityToolkitHasAdditonalWhitespace()
	{
		return useCommunityToolkitInitializationAnalyzerTests.VerifyNoErrorsWhenUseMauiCommunityToolkitHasAdditonalWhitespace();
	}

	[Benchmark]
	public Task VerifyErrorsWhenMissingUseMauiCommunityToolkit()
	{
		return useCommunityToolkitInitializationAnalyzerTests.VerifyErrorsWhenMissingUseMauiCommunityToolkit();
	}
}
