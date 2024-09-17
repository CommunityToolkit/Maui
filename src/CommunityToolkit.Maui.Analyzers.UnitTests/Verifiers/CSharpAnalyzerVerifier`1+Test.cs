using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;
public static partial class CSharpAnalyzerVerifier<TAnalyzer>
	where TAnalyzer : DiagnosticAnalyzer, new()
{
	public class Test : CSharpAnalyzerTest<TAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>
	{
		public Test()
		{
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