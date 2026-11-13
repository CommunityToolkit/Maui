using BenchmarkDotNet.Attributes;
using CommunityToolkit.Maui.Analyzers.UnitTests;

namespace CommunityToolkit.Maui.Analyzers.Benchmarks;

[MemoryDiagnoser]
public class UseMauiCommunityToolkitMediaElementInitializationAnalyzerBenchmarks
{
	static readonly UseCommunityToolkitMediaElementInitializationAnalyzerTests useCommunityToolkitMediaElementInitializationAnalyzerTests = new();

	[Benchmark]
	public Task VerifyNoErrorsWhenUseMauiCommunityToolkitMediaElement()
	{
		return useCommunityToolkitMediaElementInitializationAnalyzerTests.VerifyNoErrorsWhenUseMauiCommunityToolkitMediaElement();
	}

	[Benchmark]
	public Task VerifyNoErrorsWhenUseMauiCommunityToolkitMediaElementHasAdditionalWhitespace()
	{
		return useCommunityToolkitMediaElementInitializationAnalyzerTests.VerifyNoErrorsWhenUseMauiCommunityToolkitMediaElementHasAdditionalWhitespace();
	}

	[Benchmark]
	public Task VerifyErrorsWhenMissingUseMauiCommunityToolkitMediaElement()
	{
		return useCommunityToolkitMediaElementInitializationAnalyzerTests.VerifyErrorsWhenMissingUseMauiCommunityToolkitMediaElement();
	}
}