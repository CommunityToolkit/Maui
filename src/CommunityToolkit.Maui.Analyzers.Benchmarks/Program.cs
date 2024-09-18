using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace CommunityToolkit.Maui.Analyzers.Benchmarks;

public class Program
{
	public static void Main(string[] args)
	{
		var config = DefaultConfig.Instance;
		var summary = BenchmarkRunner.Run<CommunityToolkitMauiAnalyzerBenchmarks>(config, args);
	}
}