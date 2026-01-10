using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace CommunityToolkit.Maui.SourceGenerators.Benchmarks;

class Program
{
	public static void Main(string[] args)
	{
		var config = DefaultConfig.Instance;
		BenchmarkRunner.Run<BindablePropertyAttributeSourceGeneratorBenchmarks>(config, args);
	}
}