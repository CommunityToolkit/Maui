using CommunityToolkit.Maui.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.Analyzers.Tests
{
	//This code fix is used to make tests happy ／人◕ ‿‿ ◕人＼
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = "Dumb"), Shared]

	public class DumbCodeFix : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CheckForNotImplementedExceptionAnalyzer.DiagnosticId);

		public override Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			return Task.CompletedTask;
		}

		public override FixAllProvider? GetFixAllProvider()
		{
			return null;
		}
	}
}
