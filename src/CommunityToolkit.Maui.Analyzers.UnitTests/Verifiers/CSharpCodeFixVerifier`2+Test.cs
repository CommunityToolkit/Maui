﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;
public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
	where TAnalyzer : DiagnosticAnalyzer, new()
	where TCodeFix : CodeFixProvider, new()
{
	protected class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
	{
		public Test(params Type[] assembliesUnderTest)
		{
#if NET8_0
			ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
#else
#error ReferenceAssemblies must be updated to current version of .NET
#endif
			List<Type> typesForAssembliesUnderTest =
			[
				typeof(Microsoft.Maui.Controls.Xaml.Extensions), // Microsoft.Maui.Controls.Xaml
				typeof(MauiApp),// Microsoft.Maui.Hosting
				typeof(Application), // Microsoft.Maui.Controls
			];
			typesForAssembliesUnderTest.AddRange(assembliesUnderTest);

			foreach (Type type in typesForAssembliesUnderTest)
			{
				TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(type.Assembly.Location));
			}

			SolutionTransforms.Add((solution, projectId) =>
			{
				ArgumentNullException.ThrowIfNull(solution);

				if (solution.GetProject(projectId) is not Project project)
				{
					throw new ArgumentException("Invalid ProjectId");
				}

				var compilationOptions = project.CompilationOptions ?? throw new InvalidOperationException($"{nameof(project.CompilationOptions)} cannot be null");
				compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
					compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));

				solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

				return solution;
			});
		}
	}
}