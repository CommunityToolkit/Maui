namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extension methods for Microsoft.Maui.Graphics.Color
/// </summary>
public static class ColorConversionExtensions
{
	/// <summary>
	/// Converts this <see cref="Color"/> to a <see cref="string"/> containing the red, green and blue components.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to convert.</param>
	/// <returns>
	/// A <see cref="string"/> in the format: <c>RGB(red,green,blue)</c> where <b>red</b>, <b>green</b> and <b>blue</b> will be a value between 0 and 255.
	/// (e.g. <c>RGB(255,0,0)</c> for <see cref="Color.Red"/>).
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static string ToRgbString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"RGB({color.GetByteRed()},{color.GetByteGreen()},{color.GetByteBlue()})";
	}

	/// <summary>
	/// Converts this <see cref="Color"/> to a <see cref="string"/> containing the red, green, blue and alpha components.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to convert.</param>
	/// <returns>
	/// A <see cref="string"/> in the format: <c>RGBA(red,green,blue,alpha)</c> where <b>red</b>, <b>green</b> and <b>blue</b> will be a value between 0 and 255,
	/// and <b>alpha</b> is a value between 0 and 1. (e.g. <c>RGBA(255,0,0,1)</c> for <see cref="Color.Red"/>).
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static string ToRgbaString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"RGBA({color.GetByteRed()},{color.GetByteGreen()},{color.GetByteBlue()},{color.Alpha})";
	}

	/// <summary>
	/// Converts this <see cref="Color"/> to a <see cref="string"/> containing the red, green and blue components in hexadecimal.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to convert.</param>
	/// <returns>
	/// A <see cref="string"/> in the format: <c>#redgreenblue</c> where <b>red</b>, <b>green</b> and <b>blue</b> will be a value between 00 and FF.
	/// (e.g. <c>#FF0000</c> for <see cref="Color.Red"/>).
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static string ToHexRgbString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"#{color.GetByteRed():X2}{color.GetByteGreen():X2}{color.GetByteBlue():X2}";
	}

	/// <summary>
	/// Converts this <see cref="Color"/> to a <see cref="string"/> containing the red, green, blue and alpha components in hexadecimal.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to convert.</param>
	/// <returns>
	/// A <see cref="string"/> in the format: <c>#redgreenbluealpha</c> where <b>red</b>, <b>green</b>, <b>blue</b> and <b>alpha</b> will be a value between 00 and FF.
	/// e.g. <c>#FF000000</c> for <see cref="Color.Red"/>).
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static string ToHexRgbaString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"#{color.GetByteRed():X2}{color.GetByteGreen():X2}{color.GetByteBlue():X2}{color.GetByteAlpha():X2}";
	}

	/// <summary>
	/// Converts this <see cref="Color"/> to a <see cref="string"/> containing the red, green, blue and alpha components in hexadecimal.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to convert.</param>
	/// <returns>
	/// A <see cref="string"/> in the format: <c>#alpharedgreenblue</c> where <b>alpha</b>, <b>red</b>, <b>green</b> and <b>blue</b> will be a value between 00 and FF.
	/// e.g. <c>#00FF0000</c> for <see cref="Color.Red"/>).
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static string ToHexArgbString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"#{color.GetByteAlpha():X2}{color.GetByteRed():X2}{color.GetByteGreen():X2}{color.GetByteBlue():X2}";
	}

	/// <summary>
	/// Converts this <see cref="Color"/> to a <see cref="string"/> containing the cyan, magenta, yellow and key components.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to convert.</param>
	/// <returns>
	/// A <see cref="string"/> in the format: <c>CMYK(cyan,magenta,yellow,key)</c> where <b>cyan</b>, <b>magenta</b>, <b>yellow</b> and <b>key</b> will be a value between 0% and 100%.
	/// (e.g. <c>CMYK(0,100,100,0)</c> for <see cref="Color.Red"/>).
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static string ToCmykString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"CMYK({color.GetPercentCyan():P0},{color.GetPercentMagenta():P0},{color.GetPercentYellow():P0},{color.GetPercentBlackKey():P0})";
	}

	/// <summary>
	/// Converts this <see cref="Color"/> to a <see cref="string"/> containing the cyan, magenta, yellow, key and alpha components.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to convert.</param>
	/// <returns>
	/// A <see cref="string"/> in the format: <c>CMYKA(cyan,magenta,yellow,key,alpha)</c> where <b>cyan</b>, <b>magenta</b>, <b>yellow </b>and <b>key</b> will be a value between
	/// 0% and 100% and <b>alpha</b> will be a value between 0 and 1. (e.g. <c>CMYKA(100%,100%,0%,100%,1)</c> for <see cref="Color.Red"/>).
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static string ToCmykaString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"CMYKA({color.GetPercentCyan():P0},{color.GetPercentMagenta():P0},{color.GetPercentYellow():P0},{color.GetPercentBlackKey():P0},{color.Alpha})";
	}

	/// <summary>
	/// Converts this <see cref="Color"/> to a <see cref="string"/> containing the hue, saturation and lightness components.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to convert.</param>
	/// <returns>
	/// A <see cref="string"/> in the format: <c>HSL(hue,saturation,lightness)</c> where <b>hue</b> will be a value between 0 and 360, and <b>saturation</b> and <b>lightness</b>
	/// will be a value between 0% and 100%. (e.g. <c>HSL(0,100%,50%)</c> for <see cref="Color.Red"/>).
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static string ToHslString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"HSL({color.GetDegreeHue():0},{color.GetSaturation():P0},{color.GetLuminosity():P0})";
	}

	/// <summary>
	/// Converts this <see cref="Color"/> to a <see cref="string"/> containing the hue, saturation, lightness and alpha components.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to convert.</param>
	/// <returns>
	/// A <see cref="string"/> in the format: <c>HSLA(hue,saturation,lightness,alpha)</c> where <b>hue</b> will be a value between 0 and 360, <b>saturation</b> and <b>lightness</b>
	/// will be a value between 0% and 100%, and <b>alpha</b> will be a value between 0 and 1. (e.g. <c>HSLA(0,100%,50%,1)</c> for <see cref="Color.Red"/>).
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static string ToHslaString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"HSLA({color.GetDegreeHue():0},{color.GetSaturation():P0},{color.GetLuminosity():P0},{color.Alpha})";
	}

	/// <summary>
	/// Applies the supplied <paramref name="redComponent"/> to this <see cref="Color"/>.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to modify.</param>
	/// <param name="redComponent">The red component to apply to the existing <see cref="Color"/>. Note this value must be between 0 and 1.</param>
	/// <returns>A <see cref="Color"/> with the supplied <paramref name="redComponent"/> applied.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="redComponent"/> is <b>not</b> between 0 and 1.</exception>
	public static Color WithRed(this Color color, double redComponent)
	{
		ArgumentNullException.ThrowIfNull(color);

		return redComponent is < 0 or > 1
				? throw new ArgumentOutOfRangeException(nameof(redComponent))
				: Color.FromRgba(redComponent, color.Green, color.Blue, color.Alpha);
	}

	/// <summary>
	/// Applies the supplied <paramref name="greenComponent"/> to this <see cref="Color"/>.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to modify.</param>
	/// <param name="greenComponent">The green component to apply to the existing <see cref="Color"/>. Note this value must be between 0 and 1.</param>
	/// <returns>A <see cref="Color"/> with the supplied <paramref name="greenComponent"/> applied.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="greenComponent"/> is <b>not</b> between 0 and 1.</exception>
	public static Color WithGreen(this Color color, double greenComponent)
	{
		ArgumentNullException.ThrowIfNull(color);
		return greenComponent is < 0 or > 1
				? throw new ArgumentOutOfRangeException(nameof(greenComponent))
				: Color.FromRgba(color.Red, greenComponent, color.Blue, color.Alpha);
	}

	/// <summary>
	/// Applies the supplied <paramref name="blueComponent"/> to this <see cref="Color"/>.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to modify.</param>
	/// <param name="blueComponent">The blue component to apply to the existing <see cref="Color"/>. Note this value must be between 0 and 1.</param>
	/// <returns>A <see cref="Color"/> with the supplied <paramref name="blueComponent"/> applied.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="blueComponent"/> is <b>not</b> between 0 and 1.</exception>
	public static Color WithBlue(this Color color, double blueComponent)
	{
		ArgumentNullException.ThrowIfNull(color);

		return blueComponent is < 0 or > 1
				? throw new ArgumentOutOfRangeException(nameof(blueComponent))
				: Color.FromRgba(color.Red, color.Green, blueComponent, color.Alpha);
	}

	/// <summary>
	/// Applies the supplied <paramref name="redComponent"/> to this <see cref="Color"/>.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to modify.</param>
	/// <param name="redComponent">The red component to apply to the existing <see cref="Color"/>. Note this value must be between 0 and 255.</param>
	/// <returns>A <see cref="Color"/> with the supplied <paramref name="redComponent"/> applied.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static Color WithRed(this Color color, byte redComponent)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba((double)redComponent / 255, color.Green, color.Blue, color.Alpha);
	}

	/// <summary>
	/// Applies the supplied <paramref name="greenComponent"/> to this <see cref="Color"/>.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to modify.</param>
	/// <param name="greenComponent">The green component to apply to the existing <see cref="Color"/>. Note this value must be between 0 and 255.</param>
	/// <returns>A <see cref="Color"/> with the supplied <paramref name="greenComponent"/> applied.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static Color WithGreen(this Color color, byte greenComponent)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba(color.Red, (double)greenComponent / 255, color.Blue, color.Alpha);
	}

	/// <summary>
	/// Applies the supplied <paramref name="blueComponent"/> to this <see cref="Color"/>.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to modify.</param>
	/// <param name="blueComponent">The blue component to apply to the existing <see cref="Color"/>. Note this value must be between 0 and 255.</param>
	/// <returns>A <see cref="Color"/> with the supplied <paramref name="blueComponent"/> applied.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static Color WithBlue(this Color color, byte blueComponent)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba(color.Red, color.Green, (double)blueComponent / 255, color.Alpha);
	}

	/// <summary>
	/// Applies the supplied <paramref name="alphaComponent"/> to this <see cref="Color"/>.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to modify.</param>
	/// <param name="alphaComponent">The alpha component to apply to the existing <see cref="Color"/>. Note this value must be between 0 and 255.</param>
	/// <returns>A <see cref="Color"/> with the supplied <paramref name="alphaComponent"/> applied.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static Color WithAlpha(this Color color, byte alphaComponent)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba(color.Red, color.Green, color.Blue, (double)alphaComponent / 255);
	}

	/// <summary>
	/// Applies the supplied <paramref name="cyanComponent"/> to this <see cref="Color"/>.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to modify.</param>
	/// <param name="cyanComponent">The cyan component to apply to the existing <see cref="Color"/>. Note this value must be between 0 and 1.</param>
	/// <returns>A <see cref="Color"/> with the supplied <paramref name="cyanComponent"/> applied.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static Color WithCyan(this Color color, double cyanComponent)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba((1 - cyanComponent) * (1 - color.GetPercentBlackKey()),
								(1 - color.GetPercentMagenta()) * (1 - color.GetPercentBlackKey()),
								(1 - color.GetPercentYellow()) * (1 - color.GetPercentBlackKey()),
								color.Alpha);
	}

	/// <summary>
	/// Applies the supplied <paramref name="magentaComponent"/> to this <see cref="Color"/>.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to modify.</param>
	/// <param name="magentaComponent">The magenta component to apply to the existing <see cref="Color"/>. Note this value must be between 0 and 1.</param>
	/// <returns>A <see cref="Color"/> with the supplied <paramref name="magentaComponent"/> applied.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static Color WithMagenta(this Color color, double magentaComponent)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba((1 - color.GetPercentCyan()) * (1 - color.GetPercentBlackKey()),
								(1 - magentaComponent) * (1 - color.GetPercentBlackKey()),
								(1 - color.GetPercentYellow()) * (1 - color.GetPercentBlackKey()),
								color.Alpha);
	}

	/// <summary>
	/// Applies the supplied <paramref name="yellowComponent"/> to this <see cref="Color"/>.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to modify.</param>
	/// <param name="yellowComponent">The yellow component to apply to the existing <see cref="Color"/>. Note this value must be between 0 and 1.</param>
	/// <returns>A <see cref="Color"/> with the supplied <paramref name="yellowComponent"/> applied.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static Color WithYellow(this Color color, double yellowComponent)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba((1 - color.GetPercentCyan()) * (1 - color.GetPercentBlackKey()),
								(1 - color.GetPercentMagenta()) * (1 - color.GetPercentBlackKey()),
								(1 - yellowComponent) * (1 - color.GetPercentBlackKey()),
								color.Alpha);
	}

	/// <summary>
	/// Applies the supplied <paramref name="blackKeyComponent"/> to this <see cref="Color"/>.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to modify.</param>
	/// <param name="blackKeyComponent">The black key component to apply to the existing <see cref="Color"/>. Note this value must be between 0 and 1.</param>
	/// <returns>A <see cref="Color"/> with the supplied <paramref name="blackKeyComponent"/> applied.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static Color WithBlackKey(this Color color, double blackKeyComponent)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba((1 - color.GetPercentCyan()) * (1 - blackKeyComponent),
								(1 - color.GetPercentMagenta()) * (1 - blackKeyComponent),
								(1 - color.GetPercentYellow()) * (1 - blackKeyComponent),
								color.Alpha);
	}

	/// <summary>
	/// Gets the red component of <see cref="Color"/> as a value between 0 and 255.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to retrieve the component value from.</param>
	/// <returns>The red component of <see cref="Color"/> as a value between 0 and 255.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static byte GetByteRed(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return ToByte(color.Red * 255);
	}

	/// <summary>
	/// Gets the green component of <see cref="Color"/> as a value between 0 and 255.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to retrieve the component value from.</param>
	/// <returns>The green component of <see cref="Color"/> as a value between 0 and 255.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static byte GetByteGreen(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return ToByte(color.Green * 255);
	}

	/// <summary>
	/// Gets the blue component of <see cref="Color"/> as a value between 0 and 255.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to retrieve the component value from.</param>
	/// <returns>The blue component of <see cref="Color"/> as a value between 0 and 255.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static byte GetByteBlue(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return ToByte(color.Blue * 255);
	}

	/// <summary>
	/// Gets the alpha component of <see cref="Color"/> as a value between 0 and 255.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to retrieve the component value from.</param>
	/// <returns>The alpha component of <see cref="Color"/> as a value between 0 and 255.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static byte GetByteAlpha(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return ToByte(color.Alpha * 255);
	}

	/// <summary>
	/// Gets the hue component of <see cref="Color"/> as a value between 0 and 360.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to retrieve the component value from.</param>
	/// <returns>The hue component of <see cref="Color"/> as a value between 0 and 360.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static double GetDegreeHue(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return color.GetHue() * 360;
	}

	/// <summary>
	/// Gets the black key component of <see cref="Color"/> as a value between 0 and 1.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to retrieve the component value from.</param>
	/// <returns>The black key component of <see cref="Color"/> as a value between 0 and 1.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static float GetPercentBlackKey(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return 1 - Math.Max(Math.Max(color.Red, color.Green), color.Blue);
	}

	/// <summary>
	/// Gets the cyan component of <see cref="Color"/> as a value between 0 and 1.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to retrieve the component value from.</param>
	/// <returns>The cyan component of <see cref="Color"/> as a value between 0 and 1.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static float GetPercentCyan(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);

		return (1 - color.GetPercentBlackKey() is 0)
				? 0
				: (1 - color.Red - color.GetPercentBlackKey()) / (1 - color.GetPercentBlackKey());
	}

	/// <summary>
	/// Gets the magenta component of <see cref="Color"/> as a value between 0 and 1.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to retrieve the component value from.</param>
	/// <returns>The magenta component of <see cref="Color"/> as a value between 0 and 1.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static float GetPercentMagenta(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);

		return (1 - color.GetPercentBlackKey() is 0)
				? 0
				: (1 - color.Green - color.GetPercentBlackKey()) / (1 - color.GetPercentBlackKey());
	}

	/// <summary>
	/// Gets the yellow component of <see cref="Color"/> as a value between 0 and 1.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to retrieve the component value from.</param>
	/// <returns>The yellow component of <see cref="Color"/> as a value between 0 and 1.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static float GetPercentYellow(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return (1 - color.GetPercentBlackKey() is 0)
				? 0
				: (1 - color.Blue - color.GetPercentBlackKey()) / (1 - color.GetPercentBlackKey());
	}

	/// <summary>
	/// Inverts this <see cref="Color"/>.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to invert.</param>
	/// <returns>The inverse of this <see cref="Color"/></returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static Color ToInverseColor(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return Color.FromRgb(1 - color.Red, 1 - color.Green, 1 - color.Blue);
	}

	/// <summary>
	/// Converts this <see cref="Color"/> to a monochrome value of <see cref="Colors.Black"/> or <see cref="Colors.White"/>.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to convert.</param>
	/// <returns>
	/// This <see cref="Color"/> to a monochrome value of <see cref="Colors.Black"/> or <see cref="Colors.White"/>.
	/// If the incoming <see cref="Color"/> is light, it will be converted to <see cref="Colors.White"/>.
	/// If the incoming <see cref="Color"/> is dark, it will be converted to <see cref="Colors.Black"/>.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static Color ToBlackOrWhite(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return color.IsDark() ? Colors.Black : Colors.White;
	}

	/// <summary>
	/// Converts this <see cref="Color"/> to a monochrome value of <see cref="Colors.Black"/> or <see cref="Colors.White"/> based on
	/// whether this <see cref="Color"/> is determined as being dark for the human eye.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to convert.</param>
	/// <returns>
	/// This <see cref="Color"/> to a monochrome value of <see cref="Colors.Black"/> or <see cref="Colors.White"/>.
	/// If the incoming <see cref="Color"/> is light, it will be converted to <see cref="Colors.White"/>.
	/// If the incoming <see cref="Color"/> is dark, it will be converted to <see cref="Colors.Black"/>.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static Color ToBlackOrWhiteForText(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return color.IsDarkForTheEye() ? Colors.White : Colors.Black;
	}

	/// <summary>
	/// Converts this <see cref="Color"/> to a grayscale <see cref="Color"/>.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to convert.</param>
	/// <returns>This <see cref="Color"/> converted to a grayscale <see cref="Color"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static Color ToGrayScale(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);

		var avg = (color.Red + color.Blue + color.Green) / 3;
		return Color.FromRgb(avg, avg, avg);
	}

	/// <summary>
	/// Determines if this <see cref="Color"/> is dark for the human eye.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to check.</param>
	/// <returns>
	/// Whether this <see cref="Color"/> is dark for the human eye.
	/// Returns <c>true</c> if the <see cref="Color"/> is determined to be dark, <c> false otherwise.</c></returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static bool IsDarkForTheEye(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return (color.GetByteRed() * 0.299) + (color.GetByteGreen() * 0.587) + (color.GetByteBlue() * 0.114) <= 186;
	}

	/// <summary>
	/// Determines if this <see cref="Color"/> is dark.
	/// </summary>
	/// <param name="color">The <see cref="Color"/> to check.</param>
	/// <returns>
	/// Whether this <see cref="Color"/> is dark.
	/// Returns <c>true</c> if the <see cref="Color"/> is determined to be dark, <c> false otherwise.</c></returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="color"/> is null.</exception>
	public static bool IsDark(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return color.GetByteRed() + color.GetByteGreen() + color.GetByteBlue() <= 127 * 3;
	}

	static byte ToByte(float input)
	{
		var clampedInput = Math.Clamp(input, 0, 255);
		return (byte)Math.Round(clampedInput);
	}
}