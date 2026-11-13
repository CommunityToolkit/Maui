using BenchmarkDotNet.Attributes;
using CommunityToolkit.Maui.Analyzers.UnitTests;

namespace CommunityToolkit.Maui.Analyzers.Benchmarks;

[MemoryDiagnoser]
public class UseMauiCommunityToolkitAnalyzerBenchmarks
{
	static readonly UseCommunityToolkitInitializationAnalyzerTests useCommunityToolkitInitializationAnalyzerTests = new();

	[Benchmark]
	public Task VerifyNoErrorsWhenUseMauiCommunityToolkit()
	{
		return useCommunityToolkitInitializationAnalyzerTests.VerifyNoErrorsWhenUseMauiCommunityToolkit();
	}

	[Benchmark]
	public Task VerifyNoErrorsWhenUseMauiCommunityToolkitHasAdditionalWhitespace()
	{
		return useCommunityToolkitInitializationAnalyzerTests.VerifyNoErrorsWhenUseMauiCommunityToolkitHasAdditionalWhitespace();
	}

	[Benchmark]
	public Task VerifyErrorsWhenMissingUseMauiCommunityToolkit()
	{
		return useCommunityToolkitInitializationAnalyzerTests.VerifyErrorsWhenMissingUseMauiCommunityToolkit();
	}
}