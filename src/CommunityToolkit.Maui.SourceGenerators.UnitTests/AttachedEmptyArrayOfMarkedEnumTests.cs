using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class AttachedEmptyArrayOfMarkedEnumTests : BaseTest
{
	const string emptyArraySource =
		/* language=C#-test */
		//lang=csharp
		"""
		#pragma warning disable USR77, USR78
		using System;
		using System.Diagnostics.CodeAnalysis;
		using CommunityToolkit.Maui;
		using Microsoft.Maui.Controls;

		namespace EmptyMarkedArrays;

		[Experimental("USR77")]
		public enum ExpEnum { A }

		[Obsolete("o", DiagnosticId = "USR78")]
		public enum ObsIdEnum { A }

		[AttachedBindableProperty<object>("ExpScalar", DefaultValue = new ExpEnum[0])]
		public partial class ExpScalarHost : View { }

		[AttachedBindableProperty<object[]>("ExpNested", DefaultValue = new object[] { new ExpEnum[0] })]
		public partial class ExpNestedHost : View { }

		[AttachedBindableProperty<object>("ObsScalar", DefaultValue = new ObsIdEnum[0])]
		public partial class ObsScalarHost : View { }
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_ForEmptyArraysOfMarkedEnums(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(emptyArraySource, cultureName, "ExpScalarHost.g.cs", "ExpNestedHost.g.cs", "ObsScalarHost.g.cs");
	}

	[Theory]
	[InlineData("", "ExpScalarHost.g.cs", "USR77")]
	[InlineData("tr-TR", "ExpScalarHost.g.cs", "USR77")]
	[InlineData("az-Latn-AZ", "ExpScalarHost.g.cs", "USR77")]
	[InlineData("", "ExpNestedHost.g.cs", "USR77")]
	[InlineData("tr-TR", "ExpNestedHost.g.cs", "USR77")]
	[InlineData("az-Latn-AZ", "ExpNestedHost.g.cs", "USR77")]
	[InlineData("", "ObsScalarHost.g.cs", "USR78")]
	[InlineData("tr-TR", "ObsScalarHost.g.cs", "USR78")]
	[InlineData("az-Latn-AZ", "ObsScalarHost.g.cs", "USR78")]
	public void GeneratedPragma_ContainsTheIdOfTheEnum(string cultureName, string fileName, string expectedId)
	{
		var source = GeneratedCodeCompilation.Run(emptyArraySource, cultureName, TestContext.Current.CancellationToken).GetSource(fileName);
		var pragmaLine = source.Split('\n').First(static line => line.StartsWith("#pragma warning disable", StringComparison.Ordinal));

		Assert.Contains(expectedId, pragmaLine, StringComparison.Ordinal);
	}
}