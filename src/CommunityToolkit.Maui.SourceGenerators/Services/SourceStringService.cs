using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CommunityToolkit.Maui.SourceGenerators.Helpers;

static class SourceStringService
{
	public static void FormatText(ref string classSource, CSharpParseOptions? options = null)
	{
		options ??= CSharpParseOptions.Default;

		var sourceCode = CSharpSyntaxTree.ParseText(SourceText.From(classSource, Encoding.UTF8), options);
		var formattedRoot = (CSharpSyntaxNode)sourceCode.GetRoot().NormalizeWhitespace();
		classSource = CSharpSyntaxTree.Create(formattedRoot).ToString();
	}
}