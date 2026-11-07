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
#if NET11_0
			// Microsoft.CodeAnalysis.Testing 1.1.4 has no ReferenceAssemblies.Net.Net110 preset; use it once it ships
			ReferenceAssemblies = new Microsoft.CodeAnalysis.Testing.ReferenceAssemblies("net11.0", new Microsoft.CodeAnalysis.Testing.PackageIdentity("Microsoft.NETCore.App.Ref", "11.0.0-rc.3.26522.148"), Path.Combine("ref", "net11.0"));
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