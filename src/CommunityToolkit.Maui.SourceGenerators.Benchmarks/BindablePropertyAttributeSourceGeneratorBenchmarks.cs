using BenchmarkDotNet.Attributes;
using CommunityToolkit.Maui.SourceGenerators.Internal.UnitTests.BindablePropertyAttributeSourceGeneratorTests;

namespace CommunityToolkit.Maui.SourceGenerators.Benchmarks;

[MemoryDiagnoser]
public class BindablePropertyAttributeSourceGeneratorBenchmarks
{
	static readonly CommonUsageTests commonUsageTests = new();
	static readonly EdgeCaseTests edgeCaseTests = new();
	static readonly IntegrationTests integrationTests = new();

	[Benchmark]
	public Task GenerateBindableProperty_SimpleExample_GeneratesCorrectCode()
		=> commonUsageTests.GenerateBindableProperty_SimpleExample_GeneratesCorrectCode();

	[Benchmark]
	public Task GenerateBindableProperty_MultipleProperties_GeneratesCorrectCode()
		=> commonUsageTests.GenerateBindableProperty_MultipleProperties_GeneratesCorrectCode();

	[Benchmark]
	public Task GenerateBindableProperty_WithAllParameters_GeneratesCorrectCode()
		=> commonUsageTests.GenerateBindableProperty_WithAllParameters_GeneratesCorrectCode();

	[Benchmark]
	public Task GenerateBindableProperty_InternalClass_GeneratesCorrectCode()
		=> commonUsageTests.GenerateBindableProperty_InternalClass_GeneratesCorrectCode();

	[Benchmark]
	public Task GenerateBindableProperty_ComplexInheritanceScenario_GeneratesCorrectCode()
		=> integrationTests.GenerateBindableProperty_ComplexInheritanceScenario_GeneratesCorrectCode();

	[Benchmark]
	public Task GenerateBindableProperty_NestedClass_GeneratesCorrectCode()
		=> integrationTests.GenerateBindableProperty_NestedClass_GeneratesCorrectCode();

	[Benchmark]
	public Task GenerateBindableProperty_WithComplexDefaultValues_GeneratesCorrectCode()
		=> edgeCaseTests.GenerateBindableProperty_WithComplexDefaultValues_GeneratesCorrectCode();
}