using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public class UseCommunityToolkitAnalyzerTest : CSharpAnalyzerTest<UseCommunityToolkitInitializationAnalyzer, XUnitVerifier>
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
				var builder = MauiApp.CreateBuilder();
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

		ExpectedDiagnostics.Add(
				new DiagnosticResult(UseCommunityToolkitInitializationAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
					.WithMessage("`.UseCommunityToolkit()` is required to initalize .NET MAUI Community Toolkit")
					.WithSpan(1, 14, 1, 25));

		await RunAsync();
	}
}
