using BenchmarkDotNet.Attributes;
using CommunityToolkit.Maui.SourceGenerators.Internal.UnitTests.AttachedBindablePropertyAttributeSourceGeneratorTests;

namespace CommunityToolkit.Maui.SourceGenerators.Benchmarks;

[MemoryDiagnoser]
public class AttachedBindablePropertyAttributeSourceGeneratorBenchmarks
{
	static readonly AttachedBindablePropertyAttributeSourceGenerator_ClassAttribute_CommonUsageTests commonUsageTests = new();
	static readonly AttachedBindablePropertyAttributeSourceGenerator_ClassAttribute_EdgeCaseTests edgeCaseTests = new();
	static readonly AttachedBindablePropertyAttributeSourceGenerator_ClassAttribute_IntegrationTests integrationTests = new();

	[Benchmark]
	public Task GenerateAttachedBindableProperty_SimpleExample_GeneratesCorrectCode()
		=> commonUsageTests.GenerateAttachedBindableProperty_SimpleExample_GeneratesCorrectCode();

	[Benchmark]
	public Task GenerateAttachedBindableProperty_MultipleProperties_GeneratesCorrectCode()
		=> commonUsageTests.GenerateAttachedBindableProperty_MultipleProperties_GeneratesCorrectCode();

	[Benchmark]
	public Task GenerateAttachedBindableProperty_WithAllParameters_GeneratesCorrectCode()
		=> commonUsageTests.GenerateAttachedBindableProperty_WithAllParameters_GeneratesCorrectCode();

	[Benchmark]
	public Task GenerateAttachedBindableProperty_InternalClass_GeneratesCorrectCode()
		=> commonUsageTests.GenerateAttachedBindableProperty_InternalClass_GeneratesCorrectCode();

	[Benchmark]
	public Task GenerateAttachedBindableProperty_ComplexInheritanceScenario_GeneratesCorrectCode()
		=> integrationTests.GenerateAttachedBindableProperty_ComplexInheritanceScenario_GeneratesCorrectCode();

	[Benchmark]
	public Task GenerateAttachedBindableProperty_NestedClass_GeneratesCorrectCode()
		=> integrationTests.GenerateAttachedBindableProperty_AttributeOnNestedInnerClass_GeneratesCorrectCode();

	[Benchmark]
	public Task GenerateAttachedBindableProperty_WithComplexDefaultValues_GeneratesCorrectCode()
		=> edgeCaseTests.GenerateAttachedBindableProperty_WithComplexDefaultValues_GeneratesCorrectCode();
}