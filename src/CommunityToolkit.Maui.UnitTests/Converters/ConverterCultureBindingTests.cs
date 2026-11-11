using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

// .NET MAUI 11 passes CurrentUICulture to converters unless Binding.ConverterCulture says otherwise; these tests pin that flow end to end with a Turkish UI culture.
public class ConverterCultureBindingTests : BaseViewTest
{
	static readonly CultureInfo turkish = new("tr-TR");

	public ConverterCultureBindingTests()
	{
		Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
		Thread.CurrentThread.CurrentUICulture = turkish;
	}

	[Fact]
	public void TextCaseConverter_BindingWithoutConverterCulture_UsesCurrentUICulture()
	{
		var label = new Label { BindingContext = new TextSource("edit") };

		label.SetBinding(Label.TextProperty, new Binding(nameof(TextSource.Text), converter: new TextCaseConverter { Type = TextCaseType.Upper }));

		Assert.Equal("EDİT", label.Text);
	}

	[Fact]
	public void TextCaseConverter_BindingWithInvariantConverterCulture_UsesInvariantCasing()
	{
		var label = new Label { BindingContext = new TextSource("edit") };

		label.SetBinding(Label.TextProperty, new Binding(nameof(TextSource.Text), converter: new TextCaseConverter { Type = TextCaseType.Upper }) { ConverterCulture = CultureInfo.InvariantCulture });

		Assert.Equal("EDIT", label.Text);
	}

	[Fact]
	public void DoubleToIntConverter_BindingParsesParameterWithCurrentUICulture()
	{
		var label = new Label { BindingContext = new NumberSource(2.0) };

		label.SetBinding(Label.TextProperty, new Binding(nameof(NumberSource.Number), converter: new DoubleToIntConverter(), converterParameter: "2,5"));

		Assert.Equal("5", label.Text);
	}

	[Fact]
	public void MathExpressionConverter_BindingParsesStringWithCurrentUICulture()
	{
		var label = new Label { BindingContext = new TextSource("3,5") };

		label.SetBinding(Label.TextProperty, new Binding(nameof(TextSource.Text), converter: new MathExpressionConverter(), converterParameter: "x * 2"));

		Assert.Equal("7", label.Text);
	}

	[Fact]
	public void ColorToRgbaStringConverter_TwoWayBindingRoundTripsInTurkish()
	{
		var source = new ColorSource(new Color(1f, 0f, 0f, 0.5f));
		var entry = new Entry { BindingContext = source };

		entry.SetBinding(Entry.TextProperty, new Binding(nameof(ColorSource.Color), BindingMode.TwoWay, new ColorToRgbaStringConverter()));

		Assert.Equal("RGBA(255,0,0,0,5)", entry.Text);

		entry.Text = "RGBA(0,0,255,0,25)";

		Assert.Equal(0.25f, source.Color.Alpha, 2);
		Assert.Equal(1f, source.Color.Blue, 2);
	}

	sealed record TextSource(string Text);

	sealed record NumberSource(double Number);

	sealed class ColorSource(Color color)
	{
		public Color Color { get; set; } = color;
	}
}