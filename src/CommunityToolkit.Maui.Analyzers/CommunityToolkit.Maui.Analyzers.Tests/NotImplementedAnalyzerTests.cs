using CommunityToolkit.Maui.Analyzer;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
        public async Task TestMethod2()
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
    }
}
