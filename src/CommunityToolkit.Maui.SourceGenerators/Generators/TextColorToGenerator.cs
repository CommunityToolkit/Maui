﻿using System.Collections.Immutable;
using System.Text;
using CommunityToolkit.Maui.SourceGenerators.Extensions;
using CommunityToolkit.Maui.SourceGenerators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CommunityToolkit.Maui.SourceGenerators.Generators;

[Generator(LanguageNames.CSharp)]
class TextColorToGenerator : IIncrementalGenerator
{
	const string iTextStyleInterface = "Microsoft.Maui.ITextStyle";
	const string iAnimatableInterface = "Microsoft.Maui.Controls.IAnimatable";
	const string mauiControlsAssembly = "Microsoft.Maui.Controls";

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Get All Classes in User Library
		var userGeneratedClassesProvider = context.SyntaxProvider.CreateSyntaxProvider(
			static (syntaxNode, cancellationToken) => syntaxNode is ClassDeclarationSyntax { BaseList: not null },
			static (context, cancellationToken) => (ClassDeclarationSyntax)context.Node);

		// Get Microsoft.Maui.Controls Assembly Symbol
		var mauiControlsAssemblySymbolProvider = context.CompilationProvider.Select(
			static (compilation, token) => compilation.SourceModule.ReferencedAssemblySymbols.Single(q => q.Name == mauiControlsAssembly));

		var inputs = userGeneratedClassesProvider.Collect()
						.Combine(mauiControlsAssemblySymbolProvider)
						.Select(static (combined, cancellationToken) => (UserGeneratedClassesProvider: combined.Left, MauiControlsAssemblySymbolProvider: combined.Right))
						.Combine(context.CompilationProvider)
						.Select(static (combined, cancellationToken) => (combined.Left.UserGeneratedClassesProvider, combined.Left.MauiControlsAssemblySymbolProvider, Compilation: combined.Right));

		context.RegisterSourceOutput(inputs, (context, collectedValues) =>
		Execute(context, collectedValues.Compilation, collectedValues.UserGeneratedClassesProvider, collectedValues.MauiControlsAssemblySymbolProvider));
	}

	static void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<ClassDeclarationSyntax> userGeneratedClassesProvider, IAssemblySymbol mauiControlsAssemblySymbolProvider)
	{
		var textStyleSymbol = compilation.GetTypeByMetadataName(iTextStyleInterface);
		var iAnimatableSymbol = compilation.GetTypeByMetadataName(iAnimatableInterface);

		if (textStyleSymbol is null || iAnimatableSymbol is null)
		{
			var diag = Diagnostic.Create(TextColorToDiagnostic.MauiReferenceIsMissing, Location.None);
			context.ReportDiagnostic(diag);
			return;
		}

		var textStyleClassList = new List<(string ClassName, string ClassAcessModifier, string Namespace, string GenericArguments, string GenericConstraints)>();

		// Collect Microsoft.Maui.Controls that Implement ITextStyle
		var mauiTextStyleImplementors = mauiControlsAssemblySymbolProvider.GlobalNamespace.GetNamedTypeSymbols().Where(x => x.AllInterfaces.Contains(textStyleSymbol, SymbolEqualityComparer.Default)
				&& x.AllInterfaces.Contains(iAnimatableSymbol, SymbolEqualityComparer.Default));

		foreach (var namedTypeSymbol in mauiTextStyleImplementors)
		{
			textStyleClassList.Add((namedTypeSymbol.Name, "public", namedTypeSymbol.ContainingNamespace.ToDisplayString(), namedTypeSymbol.TypeArguments.GetGenericTypeArgumentsString(), namedTypeSymbol.GetGenericTypeConstraintsAsString()));
		}

		// Collect All Classes in User Library that Implement ITextStyle
		foreach (var classDeclarationSyntax in userGeneratedClassesProvider)
		{
			var declarationSymbol = compilation.GetSymbol<INamedTypeSymbol>(classDeclarationSyntax);
			if (declarationSymbol is null)
			{
				var diag = Diagnostic.Create(TextColorToDiagnostic.InvalidClassDeclarationSyntax, Location.None, classDeclarationSyntax.Identifier.Text);
				context.ReportDiagnostic(diag);
				continue;
			}

			// If the control is inherit from a Maui control that implements ITextStyle
			// We don't need to generate a extension method for it.
			// We just generate a method if the Control is a new implementation of ITextStyle and IAnimatable
			var doesContainSymbolBaseType = mauiTextStyleImplementors.ContainsSymbolBaseType(declarationSymbol);

			if (!doesContainSymbolBaseType
				&& declarationSymbol.AllInterfaces.Contains(textStyleSymbol, SymbolEqualityComparer.Default)
				&& declarationSymbol.AllInterfaces.Contains(iAnimatableSymbol, SymbolEqualityComparer.Default))
			{
				if (declarationSymbol.ContainingNamespace.IsGlobalNamespace)
				{
					var diag = Diagnostic.Create(TextColorToDiagnostic.GlobalNamespace, Location.None, declarationSymbol.Name);
					context.ReportDiagnostic(diag);
					continue;
				}

				var nameSpace = declarationSymbol.ContainingNamespace.ToDisplayString();

				var accessModifier = GetClassAccessModifier(declarationSymbol);

				if (accessModifier == string.Empty)
				{
					var diag = Diagnostic.Create(TextColorToDiagnostic.InvalidModifierAccess, Location.None, declarationSymbol.Name);
					context.ReportDiagnostic(diag);
					continue;
				}

				textStyleClassList.Add((declarationSymbol.Name, accessModifier, nameSpace, declarationSymbol.TypeArguments.GetGenericTypeArgumentsString(), declarationSymbol.GetGenericTypeConstraintsAsString()));
			}
		}

		var options = ((CSharpCompilation)compilation).SyntaxTrees[0].Options as CSharpParseOptions;
		foreach (var textStyleClass in textStyleClassList)
		{
			var textColorToBuilder = @"
// <auto-generated>
// See: CommunityToolkit.Maui.SourceGenerators.TextColorToGenerator

#nullable enable

using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace " + textStyleClass.Namespace + @";

" + textStyleClass.ClassAcessModifier + @" static partial class ColorAnimationExtensions_" + textStyleClass.ClassName + @"
{
	/// <summary>
	/// Animates the TextColor of an <see cref=""Microsoft.Maui.ITextStyle""/> to the given color
	/// </summary>
	/// <param name=""element""></param>
	/// <param name=""color"">The target color to animate the <see cref=""Microsoft.Maui.ITextStyle.TextColor""/> to</param>
	/// <param name=""rate"">The time, in milliseconds, between the frames of the animation</param>
	/// <param name=""length"">The duration, in milliseconds, of the animation</param>
	/// <param name=""easing"">The easing function to be used in the animation</param>
	/// <returns>Value indicating if the animation completed successfully or not</returns>
	public static Task<bool> TextColorTo" + textStyleClass.GenericArguments + "(this " + textStyleClass.Namespace + "." + textStyleClass.ClassName + textStyleClass.GenericArguments + @" element, Color color, uint rate = 16u, uint length = 250u, Easing? easing = null)
" + textStyleClass.GenericConstraints + @"
	{
		ArgumentNullException.ThrowIfNull(element);
		ArgumentNullException.ThrowIfNull(color);

		if(element is not Microsoft.Maui.ITextStyle)
			throw new ArgumentException($""Element must implement {nameof(Microsoft.Maui.ITextStyle)}"", nameof(element));

		//Although TextColor is defined as not-nullable, it CAN be null
		//If null => set it to Transparent as Animation will crash on null BackgroundColor
		element.TextColor ??= Colors.Transparent;

		var animationCompletionSource = new TaskCompletionSource<bool>();

		try
		{
			new Animation
			{
				{ 0, 1, GetRedTransformAnimation(element, color.Red) },
				{ 0, 1, GetGreenTransformAnimation(element, color.Green) },
				{ 0, 1, GetBlueTransformAnimation(element, color.Blue) },
				{ 0, 1, GetAlphaTransformAnimation(element, color.Alpha) },
			}
			.Commit(element, nameof(TextColorTo), rate, length, easing, (d, b) => animationCompletionSource.SetResult(true));
		}
		catch (ArgumentException aex)
		{
			//When creating an Animation too early in the lifecycle of the Page, i.e. in the OnAppearing method,
			//the Page might not have an 'IAnimationManager' yet, resulting in an ArgumentException.
			System.Diagnostics.Debug.WriteLine($""{aex.GetType().Name} thrown in {typeof(ColorAnimationExtensions_" + textStyleClass.ClassName + @").FullName}: {aex.Message}"");
			animationCompletionSource.SetResult(false);
		}

		return animationCompletionSource.Task;


		static Animation GetRedTransformAnimation(" + textStyleClass.Namespace + "." + textStyleClass.ClassName + textStyleClass.GenericArguments + @"  element, float targetRed) =>
			new(v => element.TextColor = element.TextColor.WithRed(v), element.TextColor.Red, targetRed);

		static Animation GetGreenTransformAnimation(" + textStyleClass.Namespace + "." + textStyleClass.ClassName + textStyleClass.GenericArguments + @"  element, float targetGreen) =>
			new(v => element.TextColor = element.TextColor.WithGreen(v), element.TextColor.Green, targetGreen);

		static Animation GetBlueTransformAnimation(" + textStyleClass.Namespace + "." + textStyleClass.ClassName + textStyleClass.GenericArguments + @"  element, float targetBlue) =>
			new(v => element.TextColor = element.TextColor.WithBlue(v), element.TextColor.Blue, targetBlue);

		static Animation GetAlphaTransformAnimation(" + textStyleClass.Namespace + "." + textStyleClass.ClassName + textStyleClass.GenericArguments + @"  element, float targetAlpha) =>
			new(v => element.TextColor = element.TextColor.WithAlpha((float)v), element.TextColor.Alpha, targetAlpha);
	}
}";
			var source = textColorToBuilder.ToString();
			SourceStringExtensions.FormatText(ref source, options);
			context.AddSource($"{textStyleClass.ClassName}TextColorTo.g.shared.cs", SourceText.From(source, Encoding.UTF8));
		}
	}

	static string GetClassAccessModifier(INamedTypeSymbol namedTypeSymbol) => namedTypeSymbol.DeclaredAccessibility switch
	{
		Accessibility.Public => "public",
		Accessibility.Internal => "internal",
		_ => string.Empty
	};
}