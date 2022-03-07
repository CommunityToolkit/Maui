using CommunityToolkit.Maui.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Collections.Immutable;
using System.Threading.Tasks;
using VerifyCS = CommunityToolkit.Maui.Analyzer.Test.CSharpCodeFixVerifier<CommunityToolkit.Maui.Analyzer.CheckFileNameAnalyzer,
	CommunityToolkit.Maui.Analyzers.Tests.DumbCodeFix>;
using System.Collections.Generic;

namespace CommunityToolkit.Maui.Analyzers.Tests
{
	[TestClass]
	public class FileNameAnalyzerTests
	{
		async Task<IEnumerable<Diagnostic>> SetupTest(string fileName)
		{
			var workspace = new AdhocWorkspace();
			var solution = workspace.CurrentSolution;

			var projectId = ProjectId.CreateNewId();

			solution = solution.AddProject(projectId, "MyTestProject", "MyTestProject", LanguageNames.CSharp);

			var source = @"
class Program
	{
		static void Main()
		{

		}
	}";
			solution = solution.AddDocument(DocumentId.CreateNewId(projectId),
				fileName,
				source);

			var project = solution.GetProject(projectId);

			_ = project ?? throw new NullReferenceException();

			project = project.AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

			var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
			_ = compilation ?? throw new NullReferenceException();

			var compilationWithAnalyzer = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new CheckFileNameAnalyzer()));

			var diagnostics = await compilationWithAnalyzer.GetAllDiagnosticsAsync().ConfigureAwait(false);

			return diagnostics.Where(x => x.Id == CheckFileNameAnalyzer.DiagnosticId);
		}

		[DataTestMethod]
		[DataRow("/Android/Bla.android.cs")]
		[DataRow("/ios/Bla.ios.cs")]
		[DataRow("/ios/Bla.macios.cs")]
		[DataRow("/Mac/Bla.macos.cs")]
		[DataRow("/Mac/Bla.shared.cs")]
		[DataRow("Bla.shared.cs")]
		[DataRow("Bla.android.cs")]
		[DataRow("Bla.ios.cs")]
		[DataRow("Bla.macios.cs")]
		[DataRow("Bla.macos.cs")]
		public async Task NoErrors(string fileName)
		{
			var diagnostics = (await SetupTest(fileName).ConfigureAwait(false)).ToArray();

			Assert.AreEqual(0, diagnostics.Length);
		}

		[DataTestMethod]
		[DataRow("/Android/Bla.cs")]
		[DataRow("/ios/Bla.cs")]
		[DataRow("/Views/ios/Bla.cs")]
		[DataRow("Bla.cs")]
		public async Task WithErrors(string fileName)
		{
			var diagnostics = (await SetupTest(fileName).ConfigureAwait(false)).ToArray();

			Assert.AreEqual(1, diagnostics.Length);
		}
	}



}
