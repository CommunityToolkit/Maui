using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public static partial class CSharpAnalyzerVerifier<TAnalyzer>
	where TAnalyzer : DiagnosticAnalyzer, new()
{
	class Test : CSharpAnalyzerTest<TAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>
	{
		public Test(params ReadOnlySpan<Type> assembliesUnderTest)
		{
#if NET10_0
			ReferenceAssemblies = Microsoft.CodeAnalysis.Testing.ReferenceAssemblies.Net.Net100;
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

			foreach (var type in typesForAssembliesUnderTest)
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