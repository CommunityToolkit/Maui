using BenchmarkDotNet.Attributes;
using CommunityToolkit.Maui.Analyzers.UnitTests;

namespace CommunityToolkit.Maui.Analyzers.Benchmarks;

[MemoryDiagnoser]
public class MaximumRatingRangeAnalyzerBenchmarks
{
    static readonly MaximumRatingRangeAnalyzerTests maximumRatingRangeAnalyzerTests = new();

    [Benchmark]
    public Task DiagnosticForInvalidMaximumRating()
    {
        return maximumRatingRangeAnalyzerTests.DiagnosticForInvalidMaximumRatings();
    }

    [Benchmark]
    public Task NoDiagnosticForValidMaximumRating()
    {
        return maximumRatingRangeAnalyzerTests.NoDiagnosticForValidMaximumRating();
    }
}