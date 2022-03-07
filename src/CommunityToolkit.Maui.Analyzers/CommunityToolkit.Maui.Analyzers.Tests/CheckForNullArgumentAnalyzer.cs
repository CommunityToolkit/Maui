using CommunityToolkit.Maui.Analyzer;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using VerifyCS = CommunityToolkit.Maui.Analyzer.Test.CSharpCodeFixVerifier<CommunityToolkit.Maui.Analyzer.CheckForNullArgumentAnalyzer,
	CommunityToolkit.Maui.Analyzers.Tests.DumbCodeFix>;
namespace CommunityToolkit.Maui.Analyzers.Tests
{
	[TestClass]
	public class CheckForNullArgumentAnalyzerTest
	{
		[TestMethod]
		public async Task TestMethod1()
		{
			var test = @"";

			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task CheckForIfIsNull()
		{
			var test = @"
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Diagnostics;

	namespace ConsoleApplication1
	{
		class TYPENAME
		{

			public void Foo(string name)
			{
				if (name is null)
				{
					throw new NullReferenceException();
				}
			}
		}
	}
";

			var diagnosticResult = VerifyCS.Diagnostic(CheckForNullArgumentAnalyzer.DiagnosticId).WithSpan(16, 5, 19, 6).WithArguments("name");
			await VerifyCS.VerifyAnalyzerAsync(test, diagnosticResult);
		}


		[TestMethod]
		public async Task CheckForNullUsingDoubleEvilsOperator()
		{
			var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
	class TYPENAME
	{

		public void Foo(string name)
		{
			_ = name ?? throw new NullReferenceException();
		}
	}
}
";

			var diagnosticResult = VerifyCS.Diagnostic(CheckForNullArgumentAnalyzer.DiagnosticId).WithSpan(16, 8, 16, 50).WithArguments("name");
			await VerifyCS.VerifyAnalyzerAsync(test, diagnosticResult);
		}
	}
}
