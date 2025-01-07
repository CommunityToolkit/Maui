namespace CommunityToolkit.Maui.Analyzers.UnitTests;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static CommunityToolkit.Maui.Analyzers.UnitTests.CSharpCodeFixVerifier<MaximumRatingRangeAnalyzer, MaximumRatingAnalyzerCodeFixProvider>;

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
		const string validCode =
			/* language=C#-test */
			//lang=csharp
			"""
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

		await VerifyMauiToolkitAnalyzer(validCode);
	}

	[Fact]
	public async Task DiagnosticForInvalidMaximumRatings()
	{
		const string invalidUpperBoundsCode =
			/* language=C#-test */
			//lang=csharp
			"""
            using System;

            namespace TestApp
            {
            	public class RatingView
            	{
            		public int MaximumRating { get; set; }
            
            		public void TestMethod()
            		{
            			MaximumRating = 11; // Invalid value
            		}
            	}
            }
            """;

		const string invalidLowerBoundsCode =
			/* language=C#-test */
			//lang=csharp
			"""
            using System;

            namespace TestApp
            {
            	public class RatingView
            	{
            		public int MaximumRating { get; set; }
            
            		public void TestMethod()
            		{
            			MaximumRating = 0; // Invalid value
            		}
            	}
            }
            """;

		await VerifyMauiToolkitAnalyzer(invalidUpperBoundsCode, Diagnostic().WithSpan(11, 4, 11, 22).WithSeverity(DiagnosticSeverity.Error).WithArguments(1, 10));
		await VerifyMauiToolkitAnalyzer(invalidLowerBoundsCode, Diagnostic().WithSpan(11, 4, 11, 21).WithSeverity(DiagnosticSeverity.Error).WithArguments(1, 10));
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