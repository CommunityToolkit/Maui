using CommunityToolkit.Maui.MediaElement.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static CommunityToolkit.Maui.Analyzers.UnitTests.CSharpCodeFixVerifier<CommunityToolkit.Maui.MediaElement.Analyzers.UseCommunityToolkitMediaElementInitializationAnalyzer, CommunityToolkit.Maui.MediaElement.Analyzers.UseCommunityToolkitMediaElementInitializationAnalyzerCodeFixProvider>;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public class UseCommunityToolkitMediaElementInitializationAnalyzerTests
{
	[Fact]
	public void UseCommunityToolkitMediaElementInitializationAnalyzerId()
	{
		Assert.Equal("MCTME001", UseCommunityToolkitMediaElementInitializationAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task VerifyNoErrorsWhenUseMauiCommunityToolkitMediaElement()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			namespace CommunityToolkit.Maui.Analyzers.UnitTests
			{
				using Microsoft.Maui.Controls.Hosting;
				using Microsoft.Maui.Hosting;
				using CommunityToolkit.Maui;
			
				public static class MauiProgram
				{
					public static MauiApp CreateMauiApp()
					{
						var builder = MauiApp.CreateBuilder();
						builder.UseMauiApp<Microsoft.Maui.Controls.Application>()
							.UseMauiCommunityToolkitMediaElement()
							.ConfigureFonts(fonts =>
							{
								fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
								fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
							});
			
						return builder.Build();
					}
				}
			}
			""";

		await VerifyMediaElementToolkitAnalyzer(source);
	}

	[Fact]
	public async Task VerifyNoErrorsWhenUseMauiCommunityToolkitMediaElementHasAdditionalWhitespace()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			namespace CommunityToolkit.Maui.Analyzers.UnitTests
			{
				using Microsoft.Maui.Controls.Hosting;
				using Microsoft.Maui.Hosting;
				using CommunityToolkit.Maui;
			
				public static class MauiProgram
				{
					public static MauiApp CreateMauiApp()
					{
						var builder = MauiApp.CreateBuilder ();
						builder.UseMauiApp<Microsoft.Maui.Controls.Application> ()
							.UseMauiCommunityToolkitMediaElement ()
							.ConfigureFonts(fonts =>
							{
								fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
								fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
							});
			
						return builder.Build ();
					}
				}
			}
			""";

		await VerifyMediaElementToolkitAnalyzer(source);
	}

	[Fact]
	public async Task VerifyErrorsWhenMissingUseMauiCommunityToolkitMediaElement()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			namespace CommunityToolkit.Maui.Analyzers.UnitTests
			{
				using Microsoft.Maui.Controls.Hosting;
				using Microsoft.Maui.Hosting;
				using CommunityToolkit.Maui;
			
				public static class MauiProgram
				{
					public static MauiApp CreateMauiApp()
					{
						var builder = MauiApp.CreateBuilder();
						builder.UseMauiApp<Microsoft.Maui.Controls.Application>()
							.ConfigureFonts(fonts =>
							{
								fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
								fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
							});
			
						return builder.Build();
					}
				}
			}
			""";

		await VerifyMediaElementToolkitAnalyzer(source, Diagnostic().WithSpan(12, 4, 12, 61).WithSeverity(DiagnosticSeverity.Error));
	}

	static Task VerifyMediaElementToolkitAnalyzer(string source, params IReadOnlyList<DiagnosticResult> diagnosticResults)
	{
		return VerifyAnalyzerAsync(
			source,
			[
				typeof(Views.MediaElement) // CommunityToolkit.Maui.MediaElement
			],
			diagnosticResults);
	}
}