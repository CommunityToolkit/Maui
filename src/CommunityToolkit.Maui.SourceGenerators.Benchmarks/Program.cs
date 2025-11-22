using BenchmarkDotNet.Running;

namespace CommunityToolkit.Maui.SourceGenerators.Benchmarks;

class Program
{
	public static void Main(string[] args)
	{
		BenchmarkRunner.Run(typeof(Program).Assembly);
	}
}
