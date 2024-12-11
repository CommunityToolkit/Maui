namespace CommunityToolkit.Maui.Analyzers.UnitTests;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static CommunityToolkit.Maui.Analyzers.UnitTests.CSharpCodeFixVerifier<MaximumRatingRangeAnalyzer, MaximumRatingCodeFixProvider>;

public class MaximumRatingRangeAnalyzerTests
{
	[Fact]
	public void UseCommunityToolkitInitializationAnalyzerId()
	{
		Assert.Equal("MCT002", MaximumRatingRangeAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task NoDiagnosticForValidMaximumRating()
	{
		const string ValidCode = /* language=C#-test */ """
using System;

namespace TestApp
{
	public class RatingView
	{
		public int MaximumRating { get; set; }

		public void TestMethod()
		{
			MaximumRating = 5; // Valid value
		}
	}
}
""";

		await VerifyMauiToolkitAnalyzer(ValidCode);
	}

	[Fact]
	public async Task DiagnosticForInvalidMaximumRating()
	{
		const string InvalidCode = /* language=C#-test */ """
using System;

namespace TestApp
{
	public class RatingView
	{
		public int MaximumRating { get; set; }

		public void TestMethod()
		{
			MaximumRating = 15; // Invalid value
		}
	}
}
""";

		await VerifyMauiToolkitAnalyzer(InvalidCode, Diagnostic().WithSpan(11, 4, 11, 22).WithSeverity(DiagnosticSeverity.Error).WithArguments(1, 10));
	}

	static Task VerifyMauiToolkitAnalyzer(string source, params DiagnosticResult[] expected)
	{
		return VerifyAnalyzerAsync(
			source,
			[
				typeof(Options), // CommunityToolkit.Maui
				typeof(Core.Options),
			],
			expected);
	}
}