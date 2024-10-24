using BenchmarkDotNet.Attributes;
using CommunityToolkit.Maui.Analyzers.UnitTests;

namespace CommunityToolkit.Maui.Analyzers.Benchmarks;

[MemoryDiagnoser]
public class UseMauiCommunityToolkitCameraAnalyzerBenchmarks
{
	static readonly UseCommunityToolkitCameraInitializationAnalyzerTests useCommunityToolkitCameraInitializationAnalyzerTests = new();

	[Benchmark]
	public Task VerifyNoErrorsWhenUseMauiCommunityToolkitCamera()
	{
		return useCommunityToolkitCameraInitializationAnalyzerTests.VerifyNoErrorsWhenUseMauiCommunityToolkitCamera();
	}

	[Benchmark]
	public Task VerifyNoErrorsWhenUseMauiCommunityToolkitCameraHasAdditionalWhitespace()
	{
		return useCommunityToolkitCameraInitializationAnalyzerTests.VerifyNoErrorsWhenUseMauiCommunityToolkitCameraHasAdditionalWhitespace();
	}

	[Benchmark]
	public Task VerifyErrorsWhenMissingUseMauiCommunityToolkitCamera()
	{
		return useCommunityToolkitCameraInitializationAnalyzerTests.VerifyErrorsWhenMissingUseMauiCommunityToolkitCamera();
	}
}