using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class TextColorToConstraintTests : BaseTest
{
	const string textStyleMembers =
		"""
		public Color TextColor { get; set; } = Colors.Black;
		public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
		public double CharacterSpacing => 0;
		""";

	const string constrainedTextTypesSource =
		/* language=C#-test */
		//lang=csharp
		$$"""
		using System;
		using System.Collections.Generic;
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		namespace ConstrainedText;

		public interface IMarker<T>
		{
		}

		public class ComparableText<T> : VisualElement, ITextStyle where T : IComparable<T>
		{
			{{textStyleMembers}}
		}

		public class EquatableText<T> : VisualElement, ITextStyle where T : IEquatable<List<string>>
		{
			{{textStyleMembers}}
		}

		public class ClassText<T> : VisualElement, ITextStyle where T : class
		{
			{{textStyleMembers}}
		}

		public class NullableClassText<T> : VisualElement, ITextStyle where T : class?
		{
			{{textStyleMembers}}
		}

		public class StructText<T> : VisualElement, ITextStyle where T : struct
		{
			{{textStyleMembers}}
		}

		public class UnmanagedText<T> : VisualElement, ITextStyle where T : unmanaged
		{
			{{textStyleMembers}}
		}

		public class NotNullText<T> : VisualElement, ITextStyle where T : notnull
		{
			{{textStyleMembers}}
		}

		public class NewText<T> : VisualElement, ITextStyle where T : new()
		{
			{{textStyleMembers}}
		}

		public class ClassAndNewText<T> : VisualElement, ITextStyle where T : class, IMarker<T>, new()
		{
			{{textStyleMembers}}
		}

		public class DependentText<T, U> : VisualElement, ITextStyle where T : U where U : IComparable<U>
		{
			{{textStyleMembers}}
		}

		public class SeveralText<TClass, TStruct, TBase> : VisualElement, ITextStyle
			where TClass : class, IList<TBase>
			where TStruct : struct, IComparable<TStruct>
			where TBase : List<TClass>, IMarker<TClass>, new()
		{
			{{textStyleMembers}}
		}
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_ForTextTypesWhoseConstraintsUseTypeArguments(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(
			constrainedTextTypesSource,
			cultureName,
			"ComparableText-1TextColorTo",
			"EquatableText-1TextColorTo",
			"ClassText-1TextColorTo",
			"NullableClassText-1TextColorTo",
			"StructText-1TextColorTo",
			"UnmanagedText-1TextColorTo",
			"NotNullText-1TextColorTo",
			"NewText-1TextColorTo",
			"ClassAndNewText-1TextColorTo",
			"DependentText-2TextColorTo",
			"SeveralText-3TextColorTo");
	}

	[Fact]
	public void GeneratedCode_RepeatsTheConstraintsWithTheirFullTypeArguments()
	{
		var result = GeneratedCodeCompilation.Run(constrainedTextTypesSource, "", TestContext.Current.CancellationToken);

		var comparable = result.GetSource("ComparableText-1TextColorTo.g.shared.cs");
		Assert.Contains("where T : global::System.IComparable<T>", comparable, StringComparison.Ordinal);

		var equatable = result.GetSource("EquatableText-1TextColorTo.g.shared.cs");
		Assert.Contains("where T : global::System.IEquatable<global::System.Collections.Generic.List<string>>", equatable, StringComparison.Ordinal);

		var several = result.GetSource("SeveralText-3TextColorTo.g.shared.cs");
		Assert.Contains("where TClass : class, global::System.Collections.Generic.IList<TBase>", several, StringComparison.Ordinal);
		Assert.Contains("where TStruct : struct, global::System.IComparable<TStruct>", several, StringComparison.Ordinal);
		Assert.Contains("where TBase : global::System.Collections.Generic.List<TClass>, global::ConstrainedText.IMarker<TClass>, new()", several, StringComparison.Ordinal);
	}
}