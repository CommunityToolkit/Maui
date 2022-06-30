using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public class UseCommunityToolkitAnalyzerTest : CSharpCodeFixTest<UseCommunityToolkitInitializationAnalyzer, 
																	UseCommunityToolkitInitializationAnalyzerCodeFixProvider, 
																	XUnitVerifier>
{
	[Fact]
	public async Task EnsureUseCommunityToolkitAnalyzerTest()
	{
		TestCode = @"
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

		FixedCode = @"
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

		ExpectedDiagnostics.Clear();

		await RunAsync();
	}
}
