using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = CommunityToolkit.Maui.Analyzer.Test.CSharpCodeFixVerifier<
	CommunityToolkit.Maui.Analyzers.CommunityToolkitInitializationAnalyzer,
	CommunityToolkit.Maui.Analyzers.CommunityToolkitInitializationAnalyzerCodeFixProvider>;

namespace CommunityToolkit.Maui.Analyzers.Test;

[TestClass]
public class CommunityToolkitMauiAnalyzerUnitTest
{
	[TestMethod]
	public async Task EnsureUseCommunityToolkitAdded()
	{
		var failingString = @"
using Microsoft.Maui.Hosting;

namespace MauiApp1;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = Microsoft.Maui.Hosting.MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont(""OpenSans-Regular.ttf"", ""OpenSansRegular"");
				fonts.AddFont(""OpenSans-Semibold.ttf"", ""OpenSansSemibold"");
			});

		return builder.Build();
	}
}";

		var fixedString = @"
using Microsoft.Maui.Hosting;

namespace MauiApp1;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont(""OpenSans-Regular.ttf"", ""OpenSansRegular"");
				fonts.AddFont(""OpenSans-Semibold.ttf"", ""OpenSansSemibold"");
			}).UseMauiCommunityToolkit();

		return builder.Build();
	}
}";

		await VerifyCS.VerifyCodeFixAsync(failingString, fixedString);
	}
}
