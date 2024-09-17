using CommunityToolkit.Maui.MediaElement.Analyzers;
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
				.UseMauiCommunityToolkitMediaElement()
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
			typeof(Views.MediaElement) // CommunityToolkit.Maui.MediaElement
		]);
	}


}