namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extension methods for Microsoft.Maui.Graphics.Color
/// </summary>
public static class ColorConversionExtensions
{
	/// <summary>
	/// Converts Color to RGB
	/// </summary>
	/// <param name="c"></param>
	/// <returns>RGB(255,255,255)</returns>
	public static string ToRgbString(this Color c) => 
		$"RGB({c.GetByteRed()},{c.GetByteGreen()},{c.GetByteBlue()})";

	/// <summary>
	/// Converts Color to RGBA
	/// </summary>
	/// <param name="c"></param>
	/// <returns>RGBA(255,255,255,1)</returns>
	public static string ToRgbaString(this Color c) =>
        $"RGBA({c.GetByteRed()},{c.GetByteGreen()},{c.GetByteBlue()},{c.Alpha})";

	/// <summary>
	/// Converts Color to Hex RGB
	/// </summary>
	/// <param name="c"></param>
	/// <returns>#FFFFFF</returns>
	public static string ToHexRgbString(this Color c) =>
		$"#{c.GetByteRed():X2}{c.GetByteGreen():X2}{c.GetByteBlue():X2}";

	/// <summary>
	/// Converts Color to Hex RGBA
	/// </summary>
	/// <param name="c"></param>
	/// <returns>#FFFFFFFF</returns>
	public static string ToHexRgbaString(this Color c) =>
		$"#{c.GetByteRed():X2}{c.GetByteGreen():X2}{c.GetByteBlue():X2}{c.GetByteAlpha():X2}";

	/// <summary>
	/// Converts Color to Hex ARGB
	/// </summary>
	/// <param name="c"></param>
	/// <returns>#FFFFFFFF</returns>
	public static string ToHexArgbString(this Color c) =>
		$"#{c.GetByteAlpha():X2}{c.GetByteRed():X2}{c.GetByteGreen():X2}{c.GetByteBlue():X2}";

	/// <summary>
	/// Converts Color to CMYK
	/// </summary>
	/// <param name="c"></param>
	/// <returns>CMYK(100%,100%,100%,100%)</returns>
	public static string ToCmykString(this Color c) =>
        $"CMYK({c.GetPercentCyan():P0},{c.GetPercentMagenta():P0},{c.GetPercentYellow():P0},{c.GetPercentBlackKey():P0})";

	/// <summary>
	/// Converts Color to CMYKA
	/// </summary>
	/// <param name="c"></param>
	/// <returns>CMYKA(100%,100%,100%,100%,1)</returns>
	public static string ToCmykaString(this Color c) =>
        $"CMYKA({c.GetPercentCyan():P0},{c.GetPercentMagenta():P0},{c.GetPercentYellow():P0},{c.GetPercentBlackKey():P0},{c.Alpha})";

	/// <summary>
	/// Converts Color to HSL
	/// </summary>
	/// <param name="c"></param>
	/// <returns>HSLA(360,100%,100%)</returns>
	public static string ToHslString(this Color c) => $"HSL({c.GetDegreeHue():0},{c.GetSaturation():P0},{c.GetLuminosity():P0})";

	/// <summary>
	/// Converts Color to HSLA
	/// </summary>
	/// <param name="c"></param>
	///  <returns>HSLA(360,100%,100%,1)</returns>
	public static string ToHslaString(this Color c) =>
        $"HSLA({c.GetDegreeHue():0},{c.GetSaturation():P0},{c.GetLuminosity():P0},{c.Alpha})";

	/// <summary>
	/// Sets Red
	/// </summary>
	/// <param name="baseColor"></param>
	/// <param name="newR"></param>
	/// <returns>Color with updated Red</returns>
	public static Color WithRed(this Color baseColor, double newR) =>
		newR < 0 || newR > 1 
			? throw new ArgumentOutOfRangeException(nameof(newR)) 
			: Color.FromRgba(newR, baseColor.Green, baseColor.Blue, baseColor.Alpha); 
		

	/// <summary>
	/// Sets Green
	/// </summary>
	/// <param name="baseColor"></param>
	/// <param name="newG"></param>
	/// <returns>Color with updated Green</returns>
	public static Color WithGreen(this Color baseColor, double newG) =>
		newG < 0 || newG > 1
			? throw new ArgumentOutOfRangeException(nameof(newG))
			: Color.FromRgba(baseColor.Red, newG, baseColor.Blue, baseColor.Alpha);

	/// <summary>
	/// Sets Blue
	/// </summary>
	/// <param name="baseColor"></param>
	/// <param name="newB"></param>
	/// <returns>Color with updated Blue</returns>
	public static Color WithBlue(this Color baseColor, double newB) =>
		newB < 0 || newB > 1
			? throw new ArgumentOutOfRangeException(nameof(newB))
			: Color.FromRgba(baseColor.Red, baseColor.Green, newB, baseColor.Alpha);

	/// <summary>
	/// Sets Red
	/// </summary>
	/// <param name="baseColor"></param>
	/// <param name="newR"></param>
	/// <returns>Color with updated red</returns>
	public static Color WithRed(this Color baseColor, byte newR) =>
		Color.FromRgba((double)newR / 255, baseColor.Green, baseColor.Blue, baseColor.Alpha);

	/// <summary>
	/// Sets Green
	/// </summary>
	/// <param name="baseColor"></param>
	/// <param name="newG"></param>
	/// <returns>Color with updated Green</returns>
	public static Color WithGreen(this Color baseColor, byte newG) =>
		Color.FromRgba(baseColor.Red, (double)newG / 255, baseColor.Blue, baseColor.Alpha);

	/// <summary>
	/// Sets Blue
	/// </summary>
	/// <param name="baseColor"></param>
	/// <param name="newB"></param>
	/// <returns>Color with updated Blue</returns>
	public static Color WithBlue(this Color baseColor, byte newB) =>
		Color.FromRgba(baseColor.Red, baseColor.Green, (double)newB / 255, baseColor.Alpha);

	/// <summary>
	/// Sets Alpha
	/// </summary>
	/// <param name="baseColor"></param>
	/// <param name="newA"></param>
	/// <returns>Color with updated alpha</returns>
	public static Color WithAlpha(this Color baseColor, byte newA) =>
		Color.FromRgba(baseColor.Red, baseColor.Green, baseColor.Blue, (double)newA / 255);

	/// <summary>
	/// Sets Cyan CMYK 
	/// </summary>
	/// <param name="baseColor"></param>
	/// <param name="newC"></param>
	/// <returns>Color with additional cyan</returns>
	public static Color WithCyan(this Color baseColor, double newC) =>
		Color.FromRgba((1 - newC) * (1 - baseColor.GetPercentBlackKey()),
					   (1 - baseColor.GetPercentMagenta()) * (1 - baseColor.GetPercentBlackKey()),
					   (1 - baseColor.GetPercentYellow()) * (1 - baseColor.GetPercentBlackKey()),
					   baseColor.Alpha);

	/// <summary>
	/// Sets Magenta CMYK Value
	/// </summary>
	/// <param name="baseColor"></param>
	/// <param name="newM"></param>
	/// <returns>Color with Magenta value</returns>
	public static Color WithMagenta(this Color baseColor, double newM) =>
		Color.FromRgba((1 - baseColor.GetPercentCyan()) * (1 - baseColor.GetPercentBlackKey()),
					   (1 - newM) * (1 - baseColor.GetPercentBlackKey()),
					   (1 - baseColor.GetPercentYellow()) * (1 - baseColor.GetPercentBlackKey()),
					   baseColor.Alpha);

	/// <summary>
	/// Sets Yellow CMYK value
	/// </summary>
	/// <param name="baseColor"></param>
	/// <param name="newY"></param>
	/// <returns>Color with Yellow value</returns>
	public static Color WithYellow(this Color baseColor, double newY) =>
		Color.FromRgba((1 - baseColor.GetPercentCyan()) * (1 - baseColor.GetPercentBlackKey()),
					   (1 - baseColor.GetPercentMagenta()) * (1 - baseColor.GetPercentBlackKey()),
					   (1 - newY) * (1 - baseColor.GetPercentBlackKey()),
					   baseColor.Alpha);

	/// <summary>
	/// Sets Black CMYK Key
	/// </summary>
	/// <param name="baseColor"></param>
	/// <param name="newK"></param>
	/// <returns>Color with Black Key</returns>
	public static Color WithBlackKey(this Color baseColor, double newK) =>
		Color.FromRgba((1 - baseColor.GetPercentCyan()) * (1 - newK),
					   (1 - baseColor.GetPercentMagenta()) * (1 - newK),
					   (1 - baseColor.GetPercentYellow()) * (1 - newK),
					   baseColor.Alpha);

	/// <summary>
	/// Gets Red
	/// </summary>
	/// <param name="c"></param>
	/// <returns>Red</returns>
	public static byte GetByteRed(this Color c) => ToByte(c.Red * 255);

	/// <summary>
	/// Gets Green
	/// </summary>
	/// <param name="c"></param>
	/// <returns>Green</returns>
	public static byte GetByteGreen(this Color c) => ToByte(c.Green * 255);

	/// <summary>
	/// Gets Blue
	/// </summary>
	/// <param name="c"></param>
	/// <returns>BLue</returns>
	public static byte GetByteBlue(this Color c) => ToByte(c.Blue * 255);

	/// <summary>
	/// Gets Alpha
	/// </summary>
	/// <param name="c"></param>
	/// <returns>Alpha</returns>
	public static byte GetByteAlpha(this Color c) => ToByte(c.Alpha * 255);

	/// <summary>
	/// Gets Degree Hue
	/// </summary>
	/// <param name="c"></param>
	/// <returns>Degree Hue</returns>
	// Hue is a degree on the color wheel from 0 to 360. 0 is red, 120 is green, 240 is blue.
	public static double GetDegreeHue(this Color c) => c.GetHue() * 360;


	/// <summary>
	/// Get percentage Black for Color
	/// </summary>
	/// <param name="c"></param>
	/// <returns>Percentage Black</returns>
	// Note : double Percent R, G and B are simply Color.R, Color.G and Color.B
	public static float GetPercentBlackKey(this Color c) => 1 - Math.Max(Math.Max(c.Red, c.Green), c.Blue);

	/// <summary>
	/// Gets percentage Cyan for Color
	/// </summary>
	/// <param name="c"></param>
	/// <returns>Percentage Cyan</returns>
	public static float GetPercentCyan(this Color c) =>
		(1- c.GetPercentBlackKey() == 0) ? 0 : 
		(1 - c.Red - c.GetPercentBlackKey()) / (1 - c.GetPercentBlackKey());

	/// <summary>
	/// Gets percentage Magenta for Color
	/// </summary>
	/// <param name="c"></param>
	/// <returns>Percentage Magenta</returns>
    public static float GetPercentMagenta(this Color c) =>
		(1 - c.GetPercentBlackKey() == 0) ? 0 :
		(1 - c.Green - c.GetPercentBlackKey()) / (1 - c.GetPercentBlackKey());

	/// <summary>
	/// G
	/// </summary>
	/// <param name="c"></param>
	/// <returns>Percentage Yellow</returns>
    public static float GetPercentYellow(this Color c) =>
		(1 - c.GetPercentBlackKey() == 0) ? 0 :
		(1 - c.Blue - c.GetPercentBlackKey()) / (1 - c.GetPercentBlackKey());

	/// <summary>
	/// Inverts the Color
	/// </summary>
	/// <param name="baseColor"></param>
	/// <returns>Inverse Color</returns>
	public static Color ToInverseColor(this Color baseColor) =>
		Color.FromRgb(1 - baseColor.Red, 1 - baseColor.Green, 1 - baseColor.Blue);

	/// <summary>
	/// Converts dark colors to Colors.Black; coonverts light colors to Colors.White
	/// </summary>
	/// <param name="baseColor"></param>
	/// <returns>Black or White Color</returns>
	public static Color ToBlackOrWhite(this Color baseColor) => baseColor.IsDark() ? Colors.Black : Colors.White;

	/// <summary>
	/// Converts Color to Colors.Black or Colors.White for text
	/// </summary>
	/// <param name="baseColor"></param>
	/// <returns>Black or White Text Color</returns>
	public static Color ToBlackOrWhiteForText(this Color baseColor) =>
		baseColor.IsDarkForTheEye() ? Colors.White : Colors.Black;

	/// <summary>
	/// Converts a Color to Grayscale
	/// </summary>
	/// <param name="baseColor"></param>
	/// <returns>Gray Scale Color</returns>
	public static Color ToGrayScale(this Color baseColor)
	{
		var avg = (baseColor.Red + baseColor.Blue + baseColor.Green) / 3;
		return Color.FromRgb(avg, avg, avg);
	}

	/// <summary>
	/// Determines if a Color is dark for the eye
	/// </summary>
	/// <param name="c"></param>
	/// <returns>Whether the Color is dark</returns>
	public static bool IsDarkForTheEye(this Color c) =>
		(c.GetByteRed() * 0.299) + (c.GetByteGreen() * 0.587) + (c.GetByteBlue() * 0.114) <= 186;

	/// <summary>
	/// Determines whether a Color is dark
	/// </summary>
	/// <param name="c"></param>
	/// <returns>Is Color Dark</returns>
	public static bool IsDark(this Color c) => c.GetByteRed() + c.GetByteGreen() + c.GetByteBlue() <= 127 * 3;

	static byte ToByte(double input)
	{
		if (input < 0)
		{
			return 0;
		}

		if (input > 255)
		{
			return 255;
		}

		return (byte)Math.Round(input);
	}
}
