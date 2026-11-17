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
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void StrictMode_ReportsTheIdsOfTheMarkedEnums(string cultureName)
	{
		var findingIds = GeneratedCodeCompilation.RunStrict(emptyArraySource, cultureName).Findings.Select(static finding => finding.Id).ToHashSet(StringComparer.Ordinal);

		Assert.Contains("USR77", findingIds);
		Assert.Contains("USR78", findingIds);
	}
}