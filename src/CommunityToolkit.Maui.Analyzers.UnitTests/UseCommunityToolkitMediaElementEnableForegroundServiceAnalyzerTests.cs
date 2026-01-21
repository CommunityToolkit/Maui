using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.MediaElement.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static CommunityToolkit.Maui.Analyzers.UnitTests.CSharpAnalyzerVerifier<CommunityToolkit.Maui.MediaElement.Analyzers.UseCommunityToolkitMediaElementEnableForegroundServiceAnalyzer>;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public class UseCommunityToolkitMediaElementEnableForegroundServiceAnalyzerTests
{
	[Fact]
	public void UseCommunityToolkitMediaElementEnableForegroundServiceAnalyzerId()
	{
		Assert.Equal("MCTME002", UseCommunityToolkitMediaElementEnableForegroundServiceAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task VerifyNoErrorsWhenEnableForegroundServiceIsProvidedAsNamedArgument()
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
							.UseMauiCommunityToolkitMediaElement(enableForegroundService: true)
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
	public async Task VerifyNoErrorsWhenEnableForegroundServiceIsProvidedAsPositionalArgument()
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
							.UseMauiCommunityToolkitMediaElement(false)
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
	public async Task VerifyNoErrorsWhenEnableForegroundServiceIsProvidedWithOptions()
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
							.UseMauiCommunityToolkitMediaElement(
								enableForegroundService: false,
								static options => { })
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
	public async Task VerifyErrorsWhenOptionsProvidedButEnableForegroundServiceIsMissing()
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
							.UseMauiCommunityToolkitMediaElement(static options => { })
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

		// When options are provided without enableForegroundService, both diagnostics are produced:
		// 1. Our analyzer detects the missing enableForegroundService parameter
		// 2. The compiler produces CS1660 because lambda is passed where bool is expected
		await VerifyMediaElementToolkitAnalyzer(source, 
			Diagnostic().WithSpan(13, 5, 13, 64).WithSeverity(DiagnosticSeverity.Error),
			DiagnosticResult.CompilerError("CS1660").WithSpan(13, 57, 13, 59).WithArguments("lambda expression", "bool"));
	}

	[Fact]
	public async Task VerifyErrorsWhenNoArgumentsProvided()
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

		// When no arguments are provided at all, both diagnostics should be produced:
		// 1. Our analyzer detects the missing enableForegroundService parameter
		// 2. The compiler produces CS7036 because required parameter 'enableForegroundService' is missing
		await VerifyMediaElementToolkitAnalyzer(source, 
		Diagnostic().WithSpan(13, 6, 13, 41).WithSeverity(DiagnosticSeverity.Error),
		DiagnosticResult.CompilerError("CS7036").WithSpan(13, 6, 13, 41).WithArguments("enableForegroundService", "CommunityToolkit.Maui.AppBuilderExtensions.UseMauiCommunityToolkitMediaElement(Microsoft.Maui.Hosting.MauiAppBuilder, bool, System.Action<CommunityToolkit.Maui.Core.MediaElementOptions>?)"));
	}

	static Task VerifyMediaElementToolkitAnalyzer(string source, params IReadOnlyList<DiagnosticResult> diagnosticResults)
	{
		return VerifyAnalyzerAsync(
			source,
			[
				typeof(MediaElementOptions) // CommunityToolkit.Maui.MediaElement
			],
			diagnosticResults);
	}
}
