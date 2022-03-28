using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CommunityToolkit.Maui.SourceGenerators.Helpers;

static class SourceStringExtensions
{
	public static void FormatText(ref string classSource, CSharpParseOptions? options)
	{
		var mysource = CSharpSyntaxTree.ParseText(SourceText.From(classSource, Encoding.UTF8), options);
		var formattedRoot = (CSharpSyntaxNode)mysource.GetRoot().NormalizeWhitespace();
		classSource = CSharpSyntaxTree.Create(formattedRoot).ToString();
	}
}