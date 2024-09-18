using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static CommunityToolkit.Maui.Analyzers.UnitTests.CSharpAnalyzerVerifier<CommunityToolkit.Maui.Analyzers.UseCommunityToolkitInitializationAnalyzer>;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public class UseCommunityToolkitInitializationAnalyzerTests
{
	[Fact]
	public void UseCommunityToolkitInitializationAnalyzerId()
	{
		Assert.Equal("MCT001", UseCommunityToolkitInitializationAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task VerifyNoErrorsWhenUseMauiCommunityToolkit()
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
				.UseMauiCommunityToolkit()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont(""OpenSans-Regular.ttf"", ""OpenSansRegular"");
					fonts.AddFont(""OpenSans-Semibold.ttf"", ""OpenSansSemibold"");
				});

			return builder.Build();
		}
	}
}";

		await VerifyMauiToolkitAnalyzer(source);
	}

	[Fact]
	public async Task VerifyNoErrorsWhenUseMauiCommunityToolkitHasAdditonalWhitespace()
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
			var builder = MauiApp.CreateBuilder ();
			builder.UseMauiApp<Microsoft.Maui.Controls.Application> ()
				.UseMauiCommunityToolkit ()
				.ConfigureFonts (fonts =>
				{
					fonts.AddFont(""OpenSans-Regular.ttf"", ""OpenSansRegular"");
					fonts.AddFont(""OpenSans-Semibold.ttf"", ""OpenSansSemibold"");
				});

			return builder.Build ();
		}
	}
}";

		await VerifyMauiToolkitAnalyzer(source);
	}

	[Fact]
	public async Task VerifyErrorsWhenMissingUseMauiCommunityToolkit()
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
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont(""OpenSans-Regular.ttf"", ""OpenSansRegular"");
					fonts.AddFont(""OpenSans-Semibold.ttf"", ""OpenSansSemibold"");
				});

			return builder.Build();
		}
	}
}";
		
		await VerifyMauiToolkitAnalyzer(source, Diagnostic().WithSpan(13, 4, 13, 61).WithSeverity(DiagnosticSeverity.Error));
	}

	static Task VerifyMauiToolkitAnalyzer(string source, params DiagnosticResult[] expected)
	{
		return VerifyAnalyzerAsync(
			source,
			[
				typeof(Options), // CommunityToolkit.Maui
				typeof(Core.Options), // CommunityToolkit.Maui.Core;
			],
			expected);
	}
}