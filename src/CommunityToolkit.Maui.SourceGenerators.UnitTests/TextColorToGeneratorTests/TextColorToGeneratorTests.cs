using System.IO;
using CommunityToolkit.Maui.SourceGenerators.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class TextColorToGeneratorTests
{
	[Fact]
	public async Task Generator_DoesNotEmitCode_ForInternalMauiControlsTypes()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		// Arrange: create a synthetic "Microsoft.Maui.Controls" assembly that
		// contains both a public and an internal type implementing ITextStyle + IAnimatable.
		const string syntheticMauiSource =
			"""
			namespace Microsoft.Maui
			{
				public interface ITextStyle
				{
					Microsoft.Maui.Graphics.Color TextColor { get; set; }
				}
			}

			namespace Microsoft.Maui.Graphics
			{
				public class Color
				{
					public float Red   { get; }
					public float Green { get; }
					public float Blue  { get; }
					public float Alpha { get; }

					public Color WithRed(float v)   => this;
					public Color WithGreen(float v) => this;
					public Color WithBlue(float v)  => this;
					public Color WithAlpha(float v) => this;
				}

				public static class Colors
				{
					public static Color Transparent => new();
				}
			}

			namespace Microsoft.Maui.Controls
			{
				public interface IAnimatable { }

				public class BindableObject
				{
					public void Animate(string name, Animation animation, uint rate, uint length, Easing easing) { }
				}

				public class Animation : System.Collections.IEnumerable
				{
					public void Add(double beginAt, double finishAt, Animation animation) { }
					public void Commit(IAnimatable owner, string name, uint rate = 16, uint length = 250, Easing easing = null, System.Action<double, bool> finished = null) { }
					System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null;
				}

				public class Easing { }

				public class View : BindableObject, Microsoft.Maui.ITextStyle, IAnimatable
				{
					public Microsoft.Maui.Graphics.Color TextColor { get; set; }
				}

				public class PublicLabel : View { }
				internal class InternalLabel : View { }
			}
			""";

		var coreRef = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

		var mauiCompilation = CSharpCompilation.Create(
			"Microsoft.Maui.Controls",
			[CSharpSyntaxTree.ParseText(syntheticMauiSource, cancellationToken: cancellationToken)],
			[coreRef],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		using var ms = new MemoryStream();
		var emitResult = mauiCompilation.Emit(ms, cancellationToken: cancellationToken);
		Assert.True(emitResult.Success,
			"Synthetic Maui assembly failed to compile: " +
			string.Join("\n", emitResult.Diagnostics));

		ms.Seek(0, SeekOrigin.Begin);
		var mauiRef = MetadataReference.CreateFromStream(ms);

		// A trivial user compilation that references the synthetic assembly.
		const string userSource =
			"""
			namespace MyApp
			{
				public class Placeholder { }
			}
			""";

		var userCompilation = CSharpCompilation.Create(
			"UserAssembly",
			[CSharpSyntaxTree.ParseText(userSource, cancellationToken: cancellationToken)],
			[coreRef, mauiRef],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		// Act: run the generator
		var generator = new TextColorToGenerator();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.RunGenerators(userCompilation, cancellationToken);

		var runResult = driver.GetRunResult();
		var generatedSources = runResult.GeneratedTrees
			.Select(t => t.GetText(cancellationToken).ToString())
			.ToList();

		// Assert: code is generated for the public type, but NOT the internal one.
		Assert.Contains(generatedSources, src => src.Contains("PublicLabel"));
		Assert.DoesNotContain(generatedSources, src => src.Contains("InternalLabel"));
	}
}