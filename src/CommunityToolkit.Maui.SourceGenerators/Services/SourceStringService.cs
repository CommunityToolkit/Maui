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

		// NormalizeWhitespace writes CRLF between tokens but keeps the newlines inside documentation comments, which come from the raw string literals of the generators and so follow the line endings of the checkout the toolkit was built from
		formattedRoot = (CSharpSyntaxNode)new DocumentationCommentNewLineRewriter().Visit(formattedRoot);
		classSource = CSharpSyntaxTree.Create(formattedRoot).ToString();
	}

	sealed class DocumentationCommentNewLineRewriter() : CSharpSyntaxRewriter(visitIntoStructuredTrivia: true)
	{
		public override SyntaxToken VisitToken(SyntaxToken token)
		{
			if (token.IsKind(SyntaxKind.XmlTextLiteralNewLineToken) && token.Text is not "\r\n")
			{
				return SyntaxFactory.XmlTextNewLine(token.LeadingTrivia, "\r\n", "\r\n", token.TrailingTrivia);
			}

			return base.VisitToken(token);
		}
	}
}