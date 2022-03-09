namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extension methods for Microsoft.Maui.Graphics.Color
/// </summary>
public static class ColorConversionExtensions
{
	/// <summary>
	/// Converts Color to RGB
	/// </summary>
	/// <param name="color"></param>
	/// <returns>RGB(255,255,255)</returns>
	public static string ToRgbString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"RGB({color.GetByteRed()},{color.GetByteGreen()},{color.GetByteBlue()})";
	}

	/// <summary>
	/// Converts Color to RGBA
	/// </summary>
	/// <param name="color"></param>
	/// <returns>RGBA(255,255,255,1)</returns>
	public static string ToRgbaString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"RGBA({color.GetByteRed()},{color.GetByteGreen()},{color.GetByteBlue()},{color.Alpha})";
	}

	/// <summary>
	/// Converts Color to Hex RGB
	/// </summary>
	/// <param name="color"></param>
	/// <returns>#FFFFFF</returns>
	public static string ToHexRgbString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"#{color.GetByteRed():X2}{color.GetByteGreen():X2}{color.GetByteBlue():X2}";
	}

	/// <summary>
	/// Converts Color to Hex RGBA
	/// </summary>
	/// <param name="color"></param>
	/// <returns>#FFFFFFFF</returns>
	public static string ToHexRgbaString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"#{color.GetByteRed():X2}{color.GetByteGreen():X2}{color.GetByteBlue():X2}{color.GetByteAlpha():X2}";
	}

	/// <summary>
	/// Converts Color to Hex ARGB
	/// </summary>
	/// <param name="color"></param>
	/// <returns>#FFFFFFFF</returns>
	public static string ToHexArgbString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"#{color.GetByteAlpha():X2}{color.GetByteRed():X2}{color.GetByteGreen():X2}{color.GetByteBlue():X2}";
	}

	/// <summary>
	/// Converts Color to CMYK
	/// </summary>
	/// <param name="color"></param>
	/// <returns>CMYK(100%,100%,100%,100%)</returns>
	public static string ToCmykString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"CMYK({color.GetPercentCyan():P0},{color.GetPercentMagenta():P0},{color.GetPercentYellow():P0},{color.GetPercentBlackKey():P0})";
	}

	/// <summary>
	/// Converts Color to CMYKA
	/// </summary>
	/// <param name="color"></param>
	/// <returns>CMYKA(100%,100%,100%,100%,1)</returns>
	public static string ToCmykaString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"CMYKA({color.GetPercentCyan():P0},{color.GetPercentMagenta():P0},{color.GetPercentYellow():P0},{color.GetPercentBlackKey():P0},{color.Alpha})";
	}

	/// <summary>
	/// Converts Color to HSL
	/// </summary>
	/// <param name="color"></param>
	/// <returns>HSLA(360,100%,100%)</returns>
	public static string ToHslString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"HSL({color.GetDegreeHue():0},{color.GetSaturation():P0},{color.GetLuminosity():P0})";
	}

	/// <summary>
	/// Converts Color to HSLA
	/// </summary>
	/// <param name="color"></param>
	///  <returns>HSLA(360,100%,100%,1)</returns>
	public static string ToHslaString(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return $"HSLA({color.GetDegreeHue():0},{color.GetSaturation():P0},{color.GetLuminosity():P0},{color.Alpha})";
	}

	/// <summary>
	/// Sets Red
	/// </summary>
	/// <param name="color"></param>
	/// <param name="newR"></param>
	/// <returns>Color with updated Red</returns>
	public static Color WithRed(this Color color, double newR)
	{
		ArgumentNullException.ThrowIfNull(color);

		return newR < 0 || newR > 1
				? throw new ArgumentOutOfRangeException(nameof(newR))
				: Color.FromRgba(newR, color.Green, color.Blue, color.Alpha);
	}


	/// <summary>
	/// Sets Green
	/// </summary>
	/// <param name="color"></param>
	/// <param name="newG"></param>
	/// <returns>Color with updated Green</returns>
	public static Color WithGreen(this Color color, double newG)
	{
		ArgumentNullException.ThrowIfNull(color);
		return newG < 0 || newG > 1
				? throw new ArgumentOutOfRangeException(nameof(newG))
				: Color.FromRgba(color.Red, newG, color.Blue, color.Alpha);
	}

	/// <summary>
	/// Sets Blue
	/// </summary>
	/// <param name="color"></param>
	/// <param name="newB"></param>
	/// <returns>Color with updated Blue</returns>
	public static Color WithBlue(this Color color, double newB)
	{
		ArgumentNullException.ThrowIfNull(color);

		return newB < 0 || newB > 1
				? throw new ArgumentOutOfRangeException(nameof(newB))
				: Color.FromRgba(color.Red, color.Green, newB, color.Alpha);
	}

	/// <summary>
	/// Sets Red
	/// </summary>
	/// <param name="color"></param>
	/// <param name="newR"></param>
	/// <returns>Color with updated red</returns>
	public static Color WithRed(this Color color, byte newR)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba((double)newR / 255, color.Green, color.Blue, color.Alpha);
	}

	/// <summary>
	/// Sets Green
	/// </summary>
	/// <param name="color"></param>
	/// <param name="newG"></param>
	/// <returns>Color with updated Green</returns>
	public static Color WithGreen(this Color color, byte newG)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba(color.Red, (double)newG / 255, color.Blue, color.Alpha);
	}

	/// <summary>
	/// Sets Blue
	/// </summary>
	/// <param name="color"></param>
	/// <param name="newB"></param>
	/// <returns>Color with updated Blue</returns>
	public static Color WithBlue(this Color color, byte newB)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba(color.Red, color.Green, (double)newB / 255, color.Alpha);
	}


	/// <summary>
	/// Sets Alpha
	/// </summary>
	/// <param name="color"></param>
	/// <param name="newA"></param>
	/// <returns>Color with updated alpha</returns>
	public static Color WithAlpha(this Color color, byte newA)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba(color.Red, color.Green, color.Blue, (double)newA / 255);
	}

	/// <summary>
	/// Sets Cyan CMYK 
	/// </summary>
	/// <param name="color"></param>
	/// <param name="newC"></param>
	/// <returns>Color with additional cyan</returns>
	public static Color WithCyan(this Color color, double newC)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba((1 - newC) * (1 - color.GetPercentBlackKey()),
								(1 - color.GetPercentMagenta()) * (1 - color.GetPercentBlackKey()),
								(1 - color.GetPercentYellow()) * (1 - color.GetPercentBlackKey()),
								color.Alpha);
	}

	/// <summary>
	/// Sets Magenta CMYK Value
	/// </summary>
	/// <param name="color"></param>
	/// <param name="newM"></param>
	/// <returns>Color with Magenta value</returns>
	public static Color WithMagenta(this Color color, double newM)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba((1 - color.GetPercentCyan()) * (1 - color.GetPercentBlackKey()),
								(1 - newM) * (1 - color.GetPercentBlackKey()),
								(1 - color.GetPercentYellow()) * (1 - color.GetPercentBlackKey()),
								color.Alpha);
	}

	/// <summary>
	/// Sets Yellow CMYK value
	/// </summary>
	/// <param name="color"></param>
	/// <param name="newY"></param>
	/// <returns>Color with Yellow value</returns>
	public static Color WithYellow(this Color color, double newY)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba((1 - color.GetPercentCyan()) * (1 - color.GetPercentBlackKey()),
								(1 - color.GetPercentMagenta()) * (1 - color.GetPercentBlackKey()),
								(1 - newY) * (1 - color.GetPercentBlackKey()),
								color.Alpha);
	}

	/// <summary>
	/// Sets Black CMYK Key
	/// </summary>
	/// <param name="color"></param>
	/// <param name="newK"></param>
	/// <returns>Color with Black Key</returns>
	public static Color WithBlackKey(this Color color, double newK)
	{
		ArgumentNullException.ThrowIfNull(color);

		return Color.FromRgba((1 - color.GetPercentCyan()) * (1 - newK),
								(1 - color.GetPercentMagenta()) * (1 - newK),
								(1 - color.GetPercentYellow()) * (1 - newK),
								color.Alpha);
	}

	/// <summary>
	/// Gets Red
	/// </summary>
	/// <param name="color"></param>
	/// <returns>Red</returns>
	public static byte GetByteRed(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return ToByte(color.Red * 255);
	}

	/// <summary>
	/// Gets Green
	/// </summary>
	/// <param name="color"></param>
	/// <returns>Green</returns>
	public static byte GetByteGreen(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return ToByte(color.Green * 255);
	}

	/// <summary>
	/// Gets Blue
	/// </summary>
	/// <param name="color"></param>
	/// <returns>BLue</returns>
	public static byte GetByteBlue(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return ToByte(color.Blue * 255);
	}

	/// <summary>
	/// Gets Alpha
	/// </summary>
	/// <param name="color"></param>
	/// <returns>Alpha</returns>
	public static byte GetByteAlpha(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return ToByte(color.Alpha * 255);
	}

	/// <summary>
	/// Gets Degree Hue
	/// </summary>
	/// <param name="color"></param>
	/// <returns>Degree Hue</returns>
	// Hue is a degree on the color wheel from 0 to 360. 0 is red, 120 is green, 240 is blue.
	public static double GetDegreeHue(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return color.GetHue() * 360;
	}


	/// <summary>
	/// Get percentage Black for Color
	/// </summary>
	/// <param name="color"></param>
	/// <returns>Percentage Black</returns>
	// Note : double Percent R, G and B are simply Color.R, Color.G and Color.B
	public static float GetPercentBlackKey(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return 1 - Math.Max(Math.Max(color.Red, color.Green), color.Blue);
	}

	/// <summary>
	/// Gets percentage Cyan for Color
	/// </summary>
	/// <param name="color"></param>
	/// <returns>Percentage Cyan</returns>
	public static float GetPercentCyan(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);

		return (1 - color.GetPercentBlackKey() is 0)
				? 0
				: (1 - color.Red - color.GetPercentBlackKey()) / (1 - color.GetPercentBlackKey());
	}

	/// <summary>
	/// Gets percentage Magenta for Color
	/// </summary>
	/// <param name="color"></param>
	/// <returns>Percentage Magenta</returns>
	public static float GetPercentMagenta(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);

		return (1 - color.GetPercentBlackKey() is 0)
				? 0
				: (1 - color.Green - color.GetPercentBlackKey()) / (1 - color.GetPercentBlackKey());
	}

	/// <summary>
	/// G
	/// </summary>
	/// <param name="color"></param>
	/// <returns>Percentage Yellow</returns>
	public static float GetPercentYellow(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return (1 - color.GetPercentBlackKey() is 0)
				? 0
				: (1 - color.Blue - color.GetPercentBlackKey()) / (1 - color.GetPercentBlackKey());
	}

	/// <summary>
	/// Inverts the Color
	/// </summary>
	/// <param name="color"></param>
	/// <returns>Inverse Color</returns>
	public static Color ToInverseColor(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return Color.FromRgb(1 - color.Red, 1 - color.Green, 1 - color.Blue);
	}

	/// <summary>
	/// Converts dark colors to Colors.Black; coonverts light colors to Colors.White
	/// </summary>
	/// <param name="color"></param>
	/// <returns>Black or White Color</returns>
	public static Color ToBlackOrWhite(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return color.IsDark() ? Colors.Black : Colors.White;
	}

	/// <summary>
	/// Converts Color to Colors.Black or Colors.White for text
	/// </summary>
	/// <param name="color"></param>
	/// <returns>Black or White Text Color</returns>
	public static Color ToBlackOrWhiteForText(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return color.IsDarkForTheEye() ? Colors.White : Colors.Black;
	}

	/// <summary>
	/// Converts a Color to Grayscale
	/// </summary>
	/// <param name="color"></param>
	/// <returns>Gray Scale Color</returns>
	public static Color ToGrayScale(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);

		var avg = (color.Red + color.Blue + color.Green) / 3;
		return Color.FromRgb(avg, avg, avg);
	}

	/// <summary>
	/// Determines if a Color is dark for the eye
	/// </summary>
	/// <param name="color"></param>
	/// <returns>Whether the Color is dark</returns>
	public static bool IsDarkForTheEye(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return (color.GetByteRed() * 0.299) + (color.GetByteGreen() * 0.587) + (color.GetByteBlue() * 0.114) <= 186;
	}

	/// <summary>
	/// Determines whether a Color is dark
	/// </summary>
	/// <param name="color"></param>
	/// <returns>Is Color Dark</returns>
	public static bool IsDark(this Color color)
	{
		ArgumentNullException.ThrowIfNull(color);
		return	color.GetByteRed() + color.GetByteGreen() + color.GetByteBlue() <= 127 * 3;
	}

	static byte ToByte(float input)
	{
		var clampedInput = Math.Clamp(input, 0, 255);
		return (byte)Math.Round(clampedInput);
	}
}