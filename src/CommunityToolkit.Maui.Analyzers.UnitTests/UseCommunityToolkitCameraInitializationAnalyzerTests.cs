using CommunityToolkit.Maui.Camera.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static CommunityToolkit.Maui.Analyzers.UnitTests.CSharpCodeFixVerifier<CommunityToolkit.Maui.Camera.Analyzers.UseCommunityToolkitCameraInitializationAnalyzer, CommunityToolkit.Maui.Camera.Analyzers.UseCommunityToolkitCameraInitializationAnalyzerCodeFixProvider>;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public class UseCommunityToolkitCameraInitializationAnalyzerTests
{
	[Fact]
	public void UseCommunityToolkitMediaElementInitializationAnalyzerId()
	{
		Assert.Equal("MCTC001", UseCommunityToolkitCameraInitializationAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task VerifyNoErrorsWhenUseMauiCommunityToolkitCamera()
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
							.UseMauiCommunityToolkitCamera()
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

		await VerifyCameraToolkitAnalyzer(source);
	}

	[Fact]
	public async Task VerifyNoErrorsWhenUseMauiCommunityToolkitCameraHasAdditionalWhitespace()
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
							.UseMauiCommunityToolkitCamera ()
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

		await VerifyCameraToolkitAnalyzer(source);
	}

	[Fact]
	public async Task VerifyErrorsWhenMissingUseMauiCommunityToolkitCamera()
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

		await VerifyCameraToolkitAnalyzer(source, Diagnostic().WithSpan(12, 4, 12, 61).WithSeverity(DiagnosticSeverity.Error));
	}

	static Task VerifyCameraToolkitAnalyzer(string source, params IReadOnlyList<DiagnosticResult> diagnosticResults)
	{
		return VerifyAnalyzerAsync(
			source,
			[
				typeof(Views.CameraView) // CommunityToolkit.Maui.Camera
			],
			diagnosticResults);
	}
}