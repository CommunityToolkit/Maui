using BenchmarkDotNet.Attributes;
using CommunityToolkit.Maui.Analyzers.UnitTests;

namespace CommunityToolkit.Maui.Analyzers.Benchmarks;

[MemoryDiagnoser]
public class CommunityToolkitMediaElementInitializationAnalyzerBenchmarks
{
	static readonly UseCommunityToolkitMediaElementInitializationAnalyzerTests useCommunityToolkitMediaElementInitializationAnalyzerTests = new();

	[Benchmark]
	public Task VerifyNoErrorsWhenUseMauiCommunityToolkitMediaElement()
	{
		return useCommunityToolkitMediaElementInitializationAnalyzerTests.VerifyNoErrorsWhenUseMauiCommunityToolkitMediaElement();
	}

	[Benchmark]
	public Task VerifyNoErrorsWhenUseMauiCommunityToolkitMediaElementHasAdditonalWhitespace()
	{
		return useCommunityToolkitMediaElementInitializationAnalyzerTests.VerifyNoErrorsWhenUseMauiCommunityToolkitMediaElementHasAdditonalWhitespace();
	}

	[Benchmark]
	public Task VerifyErrorsWhenMissingUseMauiCommunityToolkitMediaElement()
	{
		return useCommunityToolkitMediaElementInitializationAnalyzerTests.VerifyErrorsWhenMissingUseMauiCommunityToolkitMediaElement();
	}
}
