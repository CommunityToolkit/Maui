using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static CommunityToolkit.Maui.Analyzers.UnitTests.CSharpAnalyzerVerifier<CommunityToolkit.Maui.Analyzers.BindablePropertyDefaultValueCreatorAnalyzer>;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public class AttachedBindablePropertyDefaultValueCreatorAnalyzerTests
{
	[Fact]
	public void AttachedBindablePropertyDefaultValueCreatorAnalyzerId()
	{
		Assert.Equal("MCT003", BindablePropertyDefaultValueCreatorAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task VerifyNoErrorWhenDefaultValueCreatorMethodReturnsNewInstance()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
				public static partial class TestContainer
				{
					static IList<View> CreateDefaultStateViews(BindableObject bindable) => [];
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task VerifyNoErrorWhenDefaultValueCreatorMethodReturnsNewList()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
				public static partial class TestContainer
				{
					static IList<View> CreateDefaultStateViews(BindableObject bindable) => new List<View>();
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task VerifyNoErrorWhenDefaultValueCreatorMethodReturnsMethodCall()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
				public static partial class TestContainer
				{
					static IList<View> CreateDefaultStateViews(BindableObject bindable) => CreateNewList();
					
					static IList<View> CreateNewList() => new List<View>();
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task VerifyNoErrorWhenDefaultValueCreatorMethodReturnsNonStaticProperty()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<string>("Text", DefaultValueCreatorMethodName = nameof(CreateDefaultText))]
				public static partial class TestContainer
				{
					static string CreateDefaultText(BindableObject bindable) => ((TestClass)bindable).DefaultText;
				}
				
				public class TestClass : BindableObject
				{
					public string DefaultText { get; } = "Default";
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task VerifyNoErrorWhenDefaultValueCreatorMethodHasBlockBodyReturningNewInstance()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
				public static partial class TestContainer
				{
					static IList<View> CreateDefaultStateViews(BindableObject bindable)
					{
						return [];
					}
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task VerifyNoErrorWhenNoDefaultValueCreatorMethodName()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<string>("Text")]
				public static partial class TestContainer
				{
					static readonly string DefaultText = "Default";
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task VerifyErrorWhenDefaultValueCreatorMethodReturnsStaticReadonlyField()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
				public static partial class TestContainer
				{
					static readonly IList<View> DefaultStateViews = new List<View>();

					static IList<View> CreateDefaultStateViews(BindableObject bindable) => DefaultStateViews;
				}
			}
			""";

		await VerifyAnalyzerAsync(
			source,
			Diagnostic()
				.WithSpan(14, 3, 14, 92)
				.WithSeverity(DiagnosticSeverity.Warning)
				.WithArguments("CreateDefaultStateViews"));
	}

	[Fact]
	public async Task VerifyErrorWhenDefaultValueCreatorMethodReturnsStaticReadonlyProperty()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
				public static partial class TestContainer
				{
					static IList<View> DefaultStateViews { get; } = new List<View>();

					static IList<View> CreateDefaultStateViews(BindableObject bindable) => DefaultStateViews;
				}
			}
			""";

		await VerifyAnalyzerAsync(
			source,
			Diagnostic()
				.WithSpan(14, 3, 14, 92)
				.WithSeverity(DiagnosticSeverity.Warning)
				.WithArguments("CreateDefaultStateViews"));
	}

	[Fact]
	public async Task VerifyErrorWhenDefaultValueCreatorMethodReturnsCreateDefaultValueDelegateThatReturnsAStaticInstance()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateStateViewsDelegate))]
				public static partial class TestContainer
				{
					static readonly BindableProperty.CreateDefaultValueDelegate CreateStateViewsDelegate = _ => StateViewList;

					static List<View> StateViewList { get; } = [];
				}
			}
			""";

		await VerifyAnalyzerAsync(source,
			Diagnostic()
				.WithSeverity(DiagnosticSeverity.Warning)
				.WithArguments("CreateDefaultStateViews"));
	}

	[Fact]
	public async Task VerifyErrorWhenDefaultValueCreatorMethodReturnsStaticReadonlyPropertyFromDifferentClass()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
				public static partial class TestContainer
				{
					static IList<View> CreateDefaultStateViews(BindableObject bindable) => DefaultValues.StateViews;
				}

				public static class DefaultValues
				{
					public static IList<View> StateViews { get; } = new List<View>();
				}
			}
			""";

		await VerifyAnalyzerAsync(
			source,
			Diagnostic()
				.WithSpan(12, 3, 12, 99)
				.WithSeverity(DiagnosticSeverity.Warning)
				.WithArguments("CreateDefaultStateViews"));
	}

	[Fact]
	public async Task VerifyErrorWhenDefaultValueCreatorMethodHasBlockBodyReturningStaticReadonlyField()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
				public static partial class TestContainer
				{
					static readonly IList<View> DefaultStateViews = new List<View>();

					static IList<View> CreateDefaultStateViews(BindableObject bindable)
					{
						return DefaultStateViews;
					}
				}
			}
			""";

		await VerifyAnalyzerAsync(
			source,
			Diagnostic()
				.WithSpan(14, 3, 17, 4)
				.WithSeverity(DiagnosticSeverity.Warning)
				.WithArguments("CreateDefaultStateViews"));
	}

	[Fact]
	public async Task VerifyErrorWhenDefaultValueCreatorMethodHasMultipleReturnStatementsWithOneReturningStaticReadonly()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
				public static partial class TestContainer
				{
					static readonly IList<View> DefaultStateViews = new List<View>();
					
					static IList<View> CreateDefaultStateViews(BindableObject bindable)
					{
						if (bindable is null)
						{
							return DefaultStateViews;
						}
						return new List<View>();
					}
				}
			}
			""";

		await VerifyAnalyzerAsync(
			source,
			Diagnostic()
				.WithSpan(14, 3, 21, 4)
				.WithSeverity(DiagnosticSeverity.Warning)
				.WithArguments("CreateDefaultStateViews"));
	}

	[Fact]
	public async Task VerifyErrorWhenDefaultValueCreatorMethodReturnsStaticReadonlyFieldForStringType()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<string>("Text", DefaultValueCreatorMethodName = nameof(CreateDefaultText))]
				public static partial class TestContainer
				{
					static readonly string DefaultText = "Default";
					
					static string CreateDefaultText(BindableObject bindable) => DefaultText;
				}
			}
			""";

		await VerifyAnalyzerAsync(
			source,
			Diagnostic()
				.WithSpan(13, 3, 13, 75)
				.WithSeverity(DiagnosticSeverity.Warning)
				.WithArguments("CreateDefaultText"));
	}

	[Fact]
	public async Task VerifyNoErrorWhenAttributeIsNotAttachedBindableProperty()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System;
			using System.Collections.Generic;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[Obsolete("Test")]
				public static partial class TestContainer
				{
					static readonly IList<View> DefaultStateViews = new List<View>();
					
					static IList<View> CreateDefaultStateViews(BindableObject bindable) => DefaultStateViews;
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task VerifyNoErrorWhenDefaultValueCreatorMethodReturnsStaticNonReadonlyProperty()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
				public static partial class TestContainer
				{
					static IList<View> DefaultStateViews { get; set; } = new List<View>();
					
					static IList<View> CreateDefaultStateViews(BindableObject bindable) => DefaultStateViews;
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task VerifyNoErrorWhenDefaultValueCreatorMethodReturnsNonStaticReadonlyField()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
				public static partial class TestContainer
				{
					static IList<View> CreateDefaultStateViews(BindableObject bindable)
					{
						var instance = new InstanceClass();
						return instance.DefaultStateViews;
					}
				}
				
				public class InstanceClass
				{
					public readonly IList<View> DefaultStateViews = new List<View>();
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task VerifyErrorWhenDefaultValueCreatorMethodReturnsStaticReadonlyFieldWithConditionalExpression()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
				public static partial class TestContainer
				{
					static readonly IList<View> DefaultStateViews = new List<View>();
					
					static IList<View> CreateDefaultStateViews(BindableObject bindable) => bindable is not null ? DefaultStateViews : [];
				}
			}
			""";

		await VerifyAnalyzerAsync(
			source,
			Diagnostic()
				.WithSpan(14, 3, 14, 120)
				.WithSeverity(DiagnosticSeverity.Warning)
				.WithArguments("CreateDefaultStateViews"));
	}

	[Fact]
	public async Task VerifyNoErrorWhenDefaultValueCreatorMethodReturnsLiteral()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<string>("Text", DefaultValueCreatorMethodName = nameof(CreateDefaultText))]
				public static partial class TestContainer
				{
					static string CreateDefaultText(BindableObject bindable) => "Default";
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task VerifyNoErrorWhenDefaultValueCreatorMethodReturnsDefault()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews), IsNullable = true)]
				public static partial class TestContainer
				{
					static IList<View>? CreateDefaultStateViews(BindableObject bindable) => default;
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task VerifyNoErrorWhenDefaultValueCreatorMethodReturnsNull()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews), IsNullable = true)]
				public static partial class TestContainer
				{
					static IList<View>? CreateDefaultStateViews(BindableObject bindable) => null;
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task VerifyNoErrorWhenDefaultValueCreatorMethodReturnsCreateDefaultValueDelegateThatReturnsANewInstance()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[AttachedBindableProperty<IList<View>>("StateViews", DefaultValueCreatorMethodName = nameof(CreateStateViewsDelegate))]
				public static partial class TestContainer
				{
					static readonly BindableProperty.CreateDefaultValueDelegate CreateStateViewsDelegate = (x) => new List<View>();
				}
			}
			""";

		await VerifyAnalyzerAsync(source);
	}

	static Task VerifyAnalyzerAsync(string source, params IReadOnlyList<DiagnosticResult> expected)
	{
		return CSharpAnalyzerVerifier<BindablePropertyDefaultValueCreatorAnalyzer>
			.VerifyAnalyzerAsync(
			source,
			[
				typeof(Options), // CommunityToolkit.Maui
				typeof(Core.Options), // CommunityToolkit.Maui.Core
			],
			expected);
	}
}
