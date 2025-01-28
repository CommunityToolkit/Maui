using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace CommunityToolkit.Maui.Analyzers.Benchmarks;

public class Program
{
	public static void Main(string[] args)
	{
		var config = DefaultConfig.Instance;
		BenchmarkRunner.Run<UseMauiCommunityToolkitAnalyzerBenchmarks>(config, args);
		BenchmarkRunner.Run<UseMauiCommunityToolkitCameraAnalyzerBenchmarks>(config, args);
		BenchmarkRunner.Run<UseMauiCommunityToolkitMediaElementInitializationAnalyzerBenchmarks>(config, args);
	}
}