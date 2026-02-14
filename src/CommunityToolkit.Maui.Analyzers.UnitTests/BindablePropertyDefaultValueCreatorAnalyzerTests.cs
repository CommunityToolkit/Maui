using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using static CommunityToolkit.Maui.Analyzers.UnitTests.CSharpAnalyzerVerifier<CommunityToolkit.Maui.Analyzers.BindablePropertyDefaultValueCreatorAnalyzer>;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public class BindablePropertyDefaultValueCreatorAnalyzerTests
{
	[Fact]
	public void BindablePropertyDefaultValueCreatorAnalyzerId()
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
			#pragma warning disable CS9248
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
					public partial IList<View> StateViews { get; set; }
					
					static IList<View> CreateDefaultStateViews(BindableObject bindable) => [];
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? StateViewsProperty;
					public partial IList<View> StateViews { get => false ? field : (IList<View>)GetValue(StateViewsProperty); set => SetValue(StateViewsProperty, value); }
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
			#pragma warning disable CS9248
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
					public partial IList<View> StateViews { get; set; }
					
					static IList<View> CreateDefaultStateViews(BindableObject bindable) => new List<View>();
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? StateViewsProperty;
					public partial IList<View> StateViews { get => false ? field : (IList<View>)GetValue(StateViewsProperty); set => SetValue(StateViewsProperty, value); }
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
			#pragma warning disable CS9248
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
					public partial IList<View> StateViews { get; set; }
					
					static IList<View> CreateDefaultStateViews(BindableObject bindable) => CreateNewList();
					
					static IList<View> CreateNewList() => new List<View>();
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? StateViewsProperty;
					public partial IList<View> StateViews { get => false ? field : (IList<View>)GetValue(StateViewsProperty); set => SetValue(StateViewsProperty, value); }
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
			#pragma warning disable CS9248
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultText))]
					public partial string Text { get; set; }
					
					static string CreateDefaultText(BindableObject bindable) => ((TestControl)bindable).DefaultText;
					
					public string DefaultText { get; } = "Default";
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? TextProperty;
					public partial string Text { get => false ? field : (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
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
			#pragma warning disable CS9248
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
					public partial IList<View> StateViews { get; set; }
					
					static IList<View> CreateDefaultStateViews(BindableObject bindable)
					{
						return [];
					}
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? StateViewsProperty;
					public partial IList<View> StateViews { get => false ? field : (IList<View>)GetValue(StateViewsProperty); set => SetValue(StateViewsProperty, value); }
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
			#pragma warning disable CS9248
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
					public partial IList<View> StateViews { get; set; }
					
					static readonly IList<View> DefaultStateViews = new List<View>();

					static IList<View> CreateDefaultStateViews(BindableObject bindable) => DefaultStateViews;
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? StateViewsProperty;
					public partial IList<View> StateViews { get => false ? field : (IList<View>)GetValue(StateViewsProperty); set => SetValue(StateViewsProperty, value); }
				}
			}
			""";

		await VerifyAnalyzerAsync(
			source,
			Diagnostic()
				.WithSpan(17, 3, 17, 92)
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
			#pragma warning disable CS9248
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
					public partial IList<View> StateViews { get; set; }
					
					static IList<View> DefaultStateViews { get; } = new List<View>();

					static IList<View> CreateDefaultStateViews(BindableObject bindable) => DefaultStateViews;
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? StateViewsProperty;
					public partial IList<View> StateViews { get => false ? field : (IList<View>)GetValue(StateViewsProperty); set => SetValue(StateViewsProperty, value); }
				}
			}
			""";

		await VerifyAnalyzerAsync(
			source,
			Diagnostic()
				.WithSpan(17, 3, 17, 92)
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
			#pragma warning disable CS9248
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
					public partial IList<View> StateViews { get; set; }
					
					static IList<View> CreateDefaultStateViews(BindableObject bindable) => DefaultValues.StateViews;
				}

				public static class DefaultValues
				{
					public static IList<View> StateViews { get; } = new List<View>();
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? StateViewsProperty;
					public partial IList<View> StateViews { get => false ? field : (IList<View>)GetValue(StateViewsProperty); set => SetValue(StateViewsProperty, value); }
				}
			}
			""";

		await VerifyAnalyzerAsync(
			source,
			Diagnostic()
				.WithSpan(15, 3, 15, 99)
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
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
					public partial IList<View> StateViews { get; set; }
					
					static readonly IList<View> DefaultStateViews = new List<View>();

					static IList<View> CreateDefaultStateViews(BindableObject bindable)
					{
						return DefaultStateViews;
					}
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? StateViewsProperty;
					public partial IList<View> StateViews { get => false ? field : (IList<View>)GetValue(StateViewsProperty); set => SetValue(StateViewsProperty, value); }
				}
			}
			""";

		await VerifyAnalyzerAsync(
			source,
			Diagnostic()
				.WithSpan(16, 3, 19, 4)
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
			#pragma warning disable CS9248
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
					public partial IList<View> StateViews { get; set; }
					
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

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? StateViewsProperty;
					public partial IList<View> StateViews { get => false ? field : (IList<View>)GetValue(StateViewsProperty); set => SetValue(StateViewsProperty, value); }
				}
			}
			""";

		await VerifyAnalyzerAsync(
			source,
			Diagnostic()
				.WithSpan(17, 3, 24, 4)
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
			#pragma warning disable CS9248
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultText))]
					public partial string Text { get; set; }
					
					static readonly string DefaultText = "Default";
					
					static string CreateDefaultText(BindableObject bindable) => DefaultText;
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? TextProperty;
					public partial string Text { get => false ? field : (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
				}
			}
			""";

		await VerifyAnalyzerAsync(
			source,
			Diagnostic()
				.WithSpan(16, 3, 16, 75)
				.WithSeverity(DiagnosticSeverity.Warning)
				.WithArguments("CreateDefaultText"));
	}

	[Fact]
	public async Task VerifyNoErrorWhenAttributeIsNotBindableProperty()
	{
		const string source =
			/* language=C#-test */
			//lang=csharp
			"""
			#nullable enable
			#pragma warning disable MCTEXP001
			#pragma warning disable CS9248
			using System;
			using System.Collections.Generic;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				[Obsolete("Test")]
				public partial class TestControl : View
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
			#pragma warning disable CS9248
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
					public partial IList<View> StateViews { get; set; }
					
					static IList<View> DefaultStateViews { get; set; } = new List<View>();
					
					static IList<View> CreateDefaultStateViews(BindableObject bindable) => DefaultStateViews;
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? StateViewsProperty;
					public partial IList<View> StateViews { get => false ? field : (IList<View>)GetValue(StateViewsProperty); set => SetValue(StateViewsProperty, value); }
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
			#pragma warning disable CS9248
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
					public partial IList<View> StateViews { get; set; }
					
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

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? StateViewsProperty;
					public partial IList<View> StateViews { get => false ? field : (IList<View>)GetValue(StateViewsProperty); set => SetValue(StateViewsProperty, value); }
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
			#pragma warning disable CS9248
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
					public partial IList<View> StateViews { get; set; }
					
					static readonly IList<View> DefaultStateViews = new List<View>();
					
					static IList<View> CreateDefaultStateViews(BindableObject bindable) => bindable is not null ? DefaultStateViews : [];
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? StateViewsProperty;
					public partial IList<View> StateViews { get => false ? field : (IList<View>)GetValue(StateViewsProperty); set => SetValue(StateViewsProperty, value); }
				}
			}
			""";

		await VerifyAnalyzerAsync(
			source,
			Diagnostic()
				.WithSpan(17, 3, 17, 120)
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
			#pragma warning disable CS9248
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultText))]
					public partial string Text { get; set; }
					
					static string CreateDefaultText(BindableObject bindable) => "Default";
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? TextProperty;
					public partial string Text { get => false ? field : (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
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
			#pragma warning disable CS9248
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
					public partial IList<View>? StateViews { get; set; }
					
					static IList<View>? CreateDefaultStateViews(BindableObject bindable) => default;
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? StateViewsProperty;
					public partial IList<View> StateViews { get => false ? field : (IList<View>)GetValue(StateViewsProperty); set => SetValue(StateViewsProperty, value); }
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
			#pragma warning disable CS9248
			using System.Collections.Generic;
			using CommunityToolkit.Maui;
			using Microsoft.Maui.Controls;

			namespace CommunityToolkit.Maui.UnitTests
			{
				public partial class TestControl : View
				{
					[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultStateViews))]
					public partial IList<View>? StateViews { get; set; }
					
					static IList<View>? CreateDefaultStateViews(BindableObject bindable) => null;
				}

				public partial class TestControl
				{
					public static readonly global::Microsoft.Maui.Controls.BindableProperty? StateViewsProperty;
					public partial IList<View> StateViews { get => false ? field : (IList<View>)GetValue(StateViewsProperty); set => SetValue(StateViewsProperty, value); }
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
