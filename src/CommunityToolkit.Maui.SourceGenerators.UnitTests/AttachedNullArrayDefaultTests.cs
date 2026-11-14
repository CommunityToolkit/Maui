using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class AttachedNullArrayDefaultTests : BaseTest
{
	const string nullArraySource =
		/* language=C#-test */
		//lang=csharp
		"""
		#pragma warning disable USR80
		using System;
		using System.Diagnostics.CodeAnalysis;
		using CommunityToolkit.Maui;
		using Microsoft.Maui.Controls;

		namespace NullArrays;

		[Experimental("USR80")]
		public enum ExpEnum { A }

		[AttachedBindableProperty<int[]>("Ints", DefaultValue = null)]
		public partial class IntArrayHost : View { }

		[AttachedBindableProperty<string[]>("Strings", DefaultValue = null)]
		public partial class StringArrayHost : View { }

		[AttachedBindableProperty<object[]>("Objects", DefaultValue = null)]
		public partial class ObjectArrayHost : View { }

		[AttachedBindableProperty<Type[]>("Types", DefaultValue = null)]
		public partial class TypeArrayHost : View { }

		[AttachedBindableProperty<ExpEnum[]>("Enums", DefaultValue = null)]
		public partial class EnumArrayHost : View { }

		[AttachedBindableProperty<int>("Plain")]
		public partial class PlainHost : View { }
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_ForNullDefaultOfArrayProperties(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(nullArraySource, cultureName, "IntArrayHost.g.cs", "StringArrayHost.g.cs", "ObjectArrayHost.g.cs", "TypeArrayHost.g.cs", "EnumArrayHost.g.cs", "PlainHost.g.cs");
	}

	[Theory]
	[InlineData("", "IntArrayHost.g.cs", "typeof(int[]), typeof(NullArrays.IntArrayHost), null,")]
	[InlineData("tr-TR", "IntArrayHost.g.cs", "typeof(int[]), typeof(NullArrays.IntArrayHost), null,")]
	[InlineData("az-Latn-AZ", "IntArrayHost.g.cs", "typeof(int[]), typeof(NullArrays.IntArrayHost), null,")]
	[InlineData("", "StringArrayHost.g.cs", "typeof(string[]), typeof(NullArrays.StringArrayHost), null,")]
	[InlineData("tr-TR", "StringArrayHost.g.cs", "typeof(string[]), typeof(NullArrays.StringArrayHost), null,")]
	[InlineData("az-Latn-AZ", "StringArrayHost.g.cs", "typeof(string[]), typeof(NullArrays.StringArrayHost), null,")]
	[InlineData("", "ObjectArrayHost.g.cs", "typeof(object[]), typeof(NullArrays.ObjectArrayHost), null,")]
	[InlineData("tr-TR", "ObjectArrayHost.g.cs", "typeof(object[]), typeof(NullArrays.ObjectArrayHost), null,")]
	[InlineData("az-Latn-AZ", "ObjectArrayHost.g.cs", "typeof(object[]), typeof(NullArrays.ObjectArrayHost), null,")]
	[InlineData("", "TypeArrayHost.g.cs", "typeof(global::System.Type[]), typeof(NullArrays.TypeArrayHost), null,")]
	[InlineData("tr-TR", "TypeArrayHost.g.cs", "typeof(global::System.Type[]), typeof(NullArrays.TypeArrayHost), null,")]
	[InlineData("az-Latn-AZ", "TypeArrayHost.g.cs", "typeof(global::System.Type[]), typeof(NullArrays.TypeArrayHost), null,")]
	[InlineData("", "EnumArrayHost.g.cs", "typeof(global::NullArrays.ExpEnum[]), typeof(NullArrays.EnumArrayHost), null,")]
	[InlineData("tr-TR", "EnumArrayHost.g.cs", "typeof(global::NullArrays.ExpEnum[]), typeof(NullArrays.EnumArrayHost), null,")]
	[InlineData("az-Latn-AZ", "EnumArrayHost.g.cs", "typeof(global::NullArrays.ExpEnum[]), typeof(NullArrays.EnumArrayHost), null,")]
	public void GeneratedCreateAttached_PassesNullAsDefault(string cultureName, string fileName, string expectedFragment)
	{
		var source = GeneratedCodeCompilation.Run(nullArraySource, cultureName, TestContext.Current.CancellationToken).GetSource(fileName);

		Assert.Contains(expectedFragment, source, StringComparison.Ordinal);
	}
}