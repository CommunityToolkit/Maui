using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class AttachedLiteralEscapingTests : BaseTest
{
	const string escapingSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using CommunityToolkit.Maui;
		using Microsoft.Maui.Controls;

		namespace Escaping;

		[AttachedBindableProperty<char>("Quote", DefaultValue = '\'')]
		public partial class QuoteHost : View { }

		[AttachedBindableProperty<char>("Backslash", DefaultValue = '\\')]
		public partial class BackslashHost : View { }

		[AttachedBindableProperty<char>("NewLine", DefaultValue = '\n')]
		public partial class NewLineHost : View { }

		[AttachedBindableProperty<char>("LineSeparator", DefaultValue = '\u2028')]
		public partial class LineSeparatorHost : View { }

		[AttachedBindableProperty<object[]>("Chars", DefaultValue = new object[] { '\'', '\n' })]
		public partial class CharArrayHost : View { }

		[AttachedBindableProperty<string>("Text", DefaultValue = "q\"b\\s\n\t\u2028\u2029\u0085")]
		public partial class StringHost : View { }
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_ForCharAndStringDefaultsThatNeedEscaping(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(escapingSource, cultureName, "QuoteHost.g.cs", "BackslashHost.g.cs", "NewLineHost.g.cs", "LineSeparatorHost.g.cs", "CharArrayHost.g.cs", "StringHost.g.cs");
	}

	[Theory]
	[InlineData("", "QuoteHost.g.cs", "(char)'\\''")]
	[InlineData("tr-TR", "QuoteHost.g.cs", "(char)'\\''")]
	[InlineData("az-Latn-AZ", "QuoteHost.g.cs", "(char)'\\''")]
	[InlineData("", "BackslashHost.g.cs", "(char)'\\\\'")]
	[InlineData("tr-TR", "BackslashHost.g.cs", "(char)'\\\\'")]
	[InlineData("az-Latn-AZ", "BackslashHost.g.cs", "(char)'\\\\'")]
	[InlineData("", "NewLineHost.g.cs", "(char)'\\n'")]
	[InlineData("tr-TR", "NewLineHost.g.cs", "(char)'\\n'")]
	[InlineData("az-Latn-AZ", "NewLineHost.g.cs", "(char)'\\n'")]
	[InlineData("", "LineSeparatorHost.g.cs", "(char)'\\u2028'")]
	[InlineData("tr-TR", "LineSeparatorHost.g.cs", "(char)'\\u2028'")]
	[InlineData("az-Latn-AZ", "LineSeparatorHost.g.cs", "(char)'\\u2028'")]
	[InlineData("", "CharArrayHost.g.cs", "(char)('\\'')")]
	[InlineData("", "CharArrayHost.g.cs", "(char)('\\n')")]
	[InlineData("tr-TR", "CharArrayHost.g.cs", "(char)('\\'')")]
	[InlineData("tr-TR", "CharArrayHost.g.cs", "(char)('\\n')")]
	[InlineData("az-Latn-AZ", "CharArrayHost.g.cs", "(char)('\\'')")]
	[InlineData("az-Latn-AZ", "CharArrayHost.g.cs", "(char)('\\n')")]
	[InlineData("", "StringHost.g.cs", "\"q\\\"b\\\\s\\n\\t\\u2028\\u2029\\u0085\"")]
	[InlineData("tr-TR", "StringHost.g.cs", "\"q\\\"b\\\\s\\n\\t\\u2028\\u2029\\u0085\"")]
	[InlineData("az-Latn-AZ", "StringHost.g.cs", "\"q\\\"b\\\\s\\n\\t\\u2028\\u2029\\u0085\"")]
	public void GeneratedCode_WritesTheDefaultAsAnEscapedLiteral(string cultureName, string fileName, string expectedLiteral)
	{
		var source = GeneratedCodeCompilation.Run(escapingSource, cultureName, TestContext.Current.CancellationToken).GetSource(fileName);

		Assert.Contains(expectedLiteral, source, StringComparison.Ordinal);
	}
}