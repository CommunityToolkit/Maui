using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = CommunityToolkit.Maui.Analyzer.Test.CSharpCodeFixVerifier<CommunityToolkit.Maui.Analyzer.CheckForNotImplementedExceptionAnalyzer,
	CommunityToolkit.Maui.Analyzers.Tests.DumbCodeFix>;

namespace Analyzers.Tests
{
	[TestClass]
	public class NotImplementedAnalyzerTests
	{
		[TestMethod]
		public async Task TestMethod1()
		{
			var test = @"";

			await VerifyCS.VerifyAnalyzerAsync(test);
		}

		[TestMethod]
		public async Task DiagnosticForAllNotImplementedExceptionInCode()
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
            public int Bla => throw new NotImplementedException();

            public void Foo(string name)
            {
                throw new NotImplementedException();
            }
        }
    }
";
			var diagnosticResult1 = VerifyCS.Diagnostic().WithSpan(13, 31, 13, 66);
			var diagnosticResult2 = VerifyCS.Diagnostic().WithSpan(17, 17, 17, 53);


			await VerifyCS.VerifyAnalyzerAsync(test, diagnosticResult1, diagnosticResult2);
		}

		[TestMethod]
		public async Task DiagnosticForAllNotImplementedExceptionInCodeWhenInheritFromInterface()
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
        class TYPENAME : IBar
        {
            public int Bla => throw new NotImplementedException();

            public void Foo(string name)
            {
                throw new NotImplementedException();
            }
        }

        interface IBar
        {
            int Bla { get; }
            void Foo(string name);
        }
    }
";
			var diagnosticResult1 = VerifyCS.Diagnostic().WithSpan(13, 31, 13, 66);
			var diagnosticResult2 = VerifyCS.Diagnostic().WithSpan(17, 17, 17, 53);


			await VerifyCS.VerifyAnalyzerAsync(test, diagnosticResult1, diagnosticResult2);
		}

		[TestMethod]
		public async Task DiagnosticForAllNotImplementedExceptionInCodeWhenExplicityImplementTheInterface()
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
        class TYPENAME : IBar
        {
            int IBar.Bla => throw new NotImplementedException();

            void IBar.Foo(string name)
            {
                throw new NotImplementedException();
            }
        }

        interface IBar
        {
            int Bla { get; }
            void Foo(string name);
        }
    }
";
			var diagnosticResult1 = VerifyCS.Diagnostic().WithSpan(13, 29, 13, 64);
			var diagnosticResult2 = VerifyCS.Diagnostic().WithSpan(17, 17, 17, 53);


			await VerifyCS.VerifyAnalyzerAsync(test, diagnosticResult1, diagnosticResult2);
		}


		[TestMethod]
		public async Task DiagnosticForAllNotImplementedExceptionInCodeWhenImplementAbstractClass()
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
        class TYPENAME : Bar
        {
            public override int Bla => throw new NotImplementedException();

            public override void Foo(string name)
            {
                throw new NotImplementedException();
            }
        }

        abstract class Bar
        {
            public abstract int Bla { get; }
            public abstract void Foo(string name);
        }
    }
";
			var diagnosticResult1 = VerifyCS.Diagnostic().WithSpan(13, 40, 13, 75);
			var diagnosticResult2 = VerifyCS.Diagnostic().WithSpan(17, 17, 17, 53);


			await VerifyCS.VerifyAnalyzerAsync(test, diagnosticResult1, diagnosticResult2);
		}
	}
}

