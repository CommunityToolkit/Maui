using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Converters
{
    /// <summary>
    /// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="byte"/>.
    /// </summary>
    public class ColorToByteAlphaConverter : BaseConverterOneWay<Color, byte>
    {
        public override byte ConvertFrom(Color value) => value.GetByteAlpha();
    }

    /// <summary>
    /// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="byte"/>.
    /// </summary>
    public class ColorToByteRedConverter : BaseConverterOneWay<Color, byte>
    {
        public override byte ConvertFrom(Color value) => value.GetByteRed();
    }

    /// <summary>
    /// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="byte"/>.
    /// </summary>
    public class ColorToByteGreenConverter : BaseConverterOneWay<Color, byte>
    {
        public override byte ConvertFrom(Color value) => value.GetByteGreen();
    }

    /// <summary>
    /// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="byte"/>.
    /// </summary>
    public class ColorToByteBlueConverter : BaseConverterOneWay<Color, byte>
    {
        public override byte ConvertFrom(Color value) => value.GetByteBlue();
    }

    /// <summary>
    /// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="double"/>.
    /// </summary>
    public class ColorToPercentCyanConverter : BaseConverterOneWay<Color, double>
    {
        public override double ConvertFrom(Color value) => value.GetPercentCyan();
    }

    /// <summary>
    /// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="double"/>.
    /// </summary>
    public class ColorToPercentMagentaConverter : BaseConverterOneWay<Color, double>
    {
        public override double ConvertFrom(Color value) => value.GetPercentMagenta();
    }

    /// <summary>
    /// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="double"/>.
    /// </summary>
    public class ColorToPercentYellowConverter : BaseConverterOneWay<Color, double>
    {
        public override double ConvertFrom(Color value) => value.GetPercentYellow();
    }

    /// <summary>
    /// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="double"/>.
    /// </summary>
    public class ColorToBlackKeyConverter : BaseConverterOneWay<Color, double>
    {
        public override double ConvertFrom(Color value) => value.GetPercentBlackKey();
    }

    /// <summary>
    /// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="double"/>.
    /// </summary>
    // Hue is a degree on the color wheel from 0 to 360. 0 is red, 120 is green, 240 is blue.
    public class ColorToDegreeHueConverter : BaseConverterOneWay<Color, double>
    {
        public override double ConvertFrom(Color value) => value.GetDegreeHue();
    }
}