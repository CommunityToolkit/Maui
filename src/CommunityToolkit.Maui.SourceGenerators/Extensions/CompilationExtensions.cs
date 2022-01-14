using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommunityToolkit.Maui.SourceGenerators.Extensions;

static class CompilationExtensions
{
	public static TSymbol? GetSymbol<TSymbol>(this Compilation compilation, BaseTypeDeclarationSyntax declarationSyntax)
		where TSymbol : ISymbol
	{
		var model = compilation.GetSemanticModel(declarationSyntax.SyntaxTree);
		return (TSymbol?)model.GetDeclaredSymbol(declarationSyntax);
	}
}