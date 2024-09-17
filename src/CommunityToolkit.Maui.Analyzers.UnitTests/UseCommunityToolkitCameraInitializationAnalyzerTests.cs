using CommunityToolkit.Maui.Camera.Analyzers;
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
		const string source = @"
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
					fonts.AddFont(""OpenSans-Regular.ttf"", ""OpenSansRegular"");
					fonts.AddFont(""OpenSans-Semibold.ttf"", ""OpenSansSemibold"");
				});

			return builder.Build();
		}
	}
}";

		await VerifyAnalyzerAsync(source, 
		[
			typeof(Views.CameraView) // CommunityToolkit.Maui.Camera
		]);
	}
}
