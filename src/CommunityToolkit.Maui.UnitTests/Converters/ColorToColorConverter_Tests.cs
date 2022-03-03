using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class ColorToColorConverter_Tests : BaseTest
{
	public static IReadOnlyList<object?[]> Data { get; } = new[]
	{
		new object[] { Colors.DarkBlue, Colors.Black },
		//new object[] { Colors.DarkCyan, Colors.Black },
		//new object[] { Colors.DarkGrey, Colors.Black },
		//new object[] { Colors.Black, Colors.Black },
		new object[] { Colors.Brown, Colors.White },
		//new object[] { Colors.Yellow, Colors.White},
		//new object[] { Colors.Pink, Colors.White},
		//new object[] { Colors.LightBlue, Colors.White},
		//new object[] { Colors.Wheat, Colors.White},
		//new object[] { Colors.White, Colors.White},
		//new object[] { Colors.Transparent, Colors.White},
		//new object?[] { default(Color), Colors.White},
	};

	[Theory]
	[MemberData(nameof(Data))]
	public void ColorToBlackOrWhiteConverterValidArgumentsTest(Color initialColor, Color expectedColor)
	{
		var converter = new ColorToBlackOrWhiteConverter();

		var convertedColor = converter.ConvertFrom(initialColor);

		Assert.Equal(expectedColor, convertedColor);
	}

	[Fact]
	public void ColorToBlackOrWhiteConverterInvalidArgumentsTest()
	{

	}
}
/*
/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="Color"/>.
/// </summary>
public class ColorToBlackOrWhiteConverter : BaseConverterOneWay<Color, Color>
{
	/// <inheritdoc/>
	public override Color ConvertFrom(Color value) => value.ToBlackOrWhite();
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="Color"/>.
/// </summary>
public class ColorToColorForTextConverter : BaseConverterOneWay<Color, Color>
{
	/// <inheritdoc/>
	public override Color ConvertFrom(Color value) => value.ToBlackOrWhiteForText();
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="Color"/>.
/// </summary>
public class ColorToGrayScaleColorConverter : BaseConverterOneWay<Color, Color>
{
	/// <inheritdoc/>
	public override Color ConvertFrom(Color value) => value.ToGrayScale();
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="Color"/>.
/// </summary>
public class ColorToInverseColorConverter : BaseConverterOneWay<Color, Color>
{
	/// <inheritdoc/>
	public override Color ConvertFrom(Color value) => value.ToInverseColor();
}
*/
