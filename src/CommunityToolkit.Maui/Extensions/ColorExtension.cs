using Microsoft.Maui.Graphics;
using System;

namespace CommunityToolkit.Maui.Extensions
{
    public static class ColorExtension
    {
        /// <returns>RGB(255, 255, 255)</returns>
        public static string ToRgbString(this Color c) => $"RGB({c.GetByteRed()},{c.GetByteGreen()},{c.GetByteBlue()})";

        /// <returns>RGBA(255, 255, 255, 255)</returns>
        public static string ToRgbaString(this Color c) =>
            $"RGBA({c.GetByteRed()}, {c.GetByteGreen()}, {c.GetByteBlue()}, {c.GetByteAlpha()})";

        /// <returns>#FFFFFF</returns>
        public static string ToHexRgbString(this Color c) =>
            $"#{c.GetByteRed():X2}{c.GetByteGreen():X2}{c.GetByteBlue():X2}";

        /// <returns>#FFFFFFFF</returns>
        public static string ToHexRgbaString(this Color c) =>
            $"#{c.GetByteRed():X2}{c.GetByteGreen():X2}{c.GetByteBlue():X2}{c.GetByteAlpha():X2}";

        /// <returns>CMYK(100%,100%,100%,100%)</returns>
        public static string ToCmykString(this Color c) =>
            $"CMYK({c.GetPercentCyan():P},{c.GetPercentMagenta():P},{c.GetPercentYellow():P},{c.GetPercentBlackKey():P})";

        /// <returns>CMYK(100%,100%,100%,100%,100%)</returns>
        public static string ToCmykaString(this Color c) =>
            $"CMYKA({c.GetPercentCyan():P},{c.GetPercentMagenta():P},{c.GetPercentYellow():P},{c.GetPercentBlackKey():P},{c.Alpha:P})";

        /// <returns>HSLA(360,100%,100%)</returns>
        public static string ToHslString(this Color c) => $"HSL({c.GetHue():P},{c.GetSaturation():P},{c.GetLuminosity():P})";

        /// <returns>HSLA(360°,100%,100%,100%)</returns>
        public static string ToHslaString(this Color c) =>
            $"HSLA({c.GetDegreeHue()}°,{c.GetSaturation():P},{c.GetLuminosity():P},{c.Alpha:P})";

        public static Color WithRed(this Color baseColor, double newR) =>
            Color.FromRgba(newR, baseColor.Green, baseColor.Blue, baseColor.Alpha);

        public static Color WithGreen(this Color baseColor, double newG) =>
            Color.FromRgba(baseColor.Red, newG, baseColor.Blue, baseColor.Alpha);

        public static Color WithBlue(this Color baseColor, double newB) =>
            Color.FromRgba(baseColor.Red, baseColor.Green, newB, baseColor.Alpha);

        public static Color WithAlpha(this Color baseColor, double newA) =>
            Color.FromRgba(baseColor.Red, baseColor.Green, baseColor.Blue, newA);

        public static Color WithRed(this Color baseColor, byte newR) =>
            Color.FromRgba((double)newR / 255, baseColor.Green, baseColor.Blue, baseColor.Alpha);

        public static Color WithGreen(this Color baseColor, byte newG) =>
            Color.FromRgba(baseColor.Red, (double)newG / 255, baseColor.Blue, baseColor.Alpha);

        public static Color WithBlue(this Color baseColor, byte newB) =>
            Color.FromRgba(baseColor.Red, baseColor.Green, (double)newB / 255, baseColor.Alpha);

        public static Color WithAlpha(this Color baseColor, byte newA) =>
            Color.FromRgba(baseColor.Red, baseColor.Green, baseColor.Blue, (double)newA / 255);

        public static Color WithCyan(this Color baseColor, double newC) =>
            Color.FromRgba((1 - newC) * (1 - baseColor.GetPercentBlackKey()),
                           (1 - baseColor.GetPercentMagenta()) * (1 - baseColor.GetPercentBlackKey()),
                           (1 - baseColor.GetPercentYellow()) * (1 - baseColor.GetPercentBlackKey()),
                           baseColor.Alpha);

        public static Color WithMagenta(this Color baseColor, double newM) =>
            Color.FromRgba((1 - baseColor.GetPercentCyan()) * (1 - baseColor.GetPercentBlackKey()),
                           (1 - newM) * (1 - baseColor.GetPercentBlackKey()),
                           (1 - baseColor.GetPercentYellow()) * (1 - baseColor.GetPercentBlackKey()),
                           baseColor.Alpha);

        public static Color WithYellow(this Color baseColor, double newY) =>
            Color.FromRgba((1 - baseColor.GetPercentCyan()) * (1 - baseColor.GetPercentBlackKey()),
                           (1 - baseColor.GetPercentMagenta()) * (1 - baseColor.GetPercentBlackKey()),
                           (1 - newY) * (1 - baseColor.GetPercentBlackKey()),
                           baseColor.Alpha);

        public static Color WithBlackKey(this Color baseColor, double newK) =>
            Color.FromRgba((1 - baseColor.GetPercentCyan()) * (1 - newK),
                           (1 - baseColor.GetPercentMagenta()) * (1 - newK),
                           (1 - baseColor.GetPercentYellow()) * (1 - newK),
                           baseColor.Alpha);

        public static byte GetByteRed(this Color c) => ToByte(c.Red * 255);

        public static byte GetByteGreen(this Color c) => ToByte(c.Green * 255);

        public static byte GetByteBlue(this Color c) => ToByte(c.Blue * 255);

        public static byte GetByteAlpha(this Color c) => ToByte(c.Alpha * 255);

        // Hue is a degree on the color wheel from 0 to 360. 0 is red, 120 is green, 240 is blue.
        public static double GetDegreeHue(this Color c) => c.GetHue() * 360;

        // Note : double Percent R, G and B are simply Color.R, Color.G and Color.B

        public static double GetPercentBlackKey(this Color c) => 1 - Math.Max(Math.Max(c.Red, c.Green), c.Blue);

        public static double GetPercentCyan(this Color c) =>
            (1 - c.Red - c.GetPercentBlackKey()) / (1 - c.GetPercentBlackKey());

        public static double GetPercentMagenta(this Color c) =>
            (1 - c.Green - c.GetPercentBlackKey()) / (1 - c.GetPercentBlackKey());

        public static double GetPercentYellow(this Color c) =>
            (1 - c.Blue - c.GetPercentBlackKey()) / (1 - c.GetPercentBlackKey());

        public static Color ToInverseColor(this Color baseColor) =>
            Color.FromRgb(1 - baseColor.Red, 1 - baseColor.Green, 1 - baseColor.Blue);

        public static Color ToBlackOrWhite(this Color baseColor) => baseColor.IsDark() ? Colors.Black : Colors.White;

        public static Color ToBlackOrWhiteForText(this Color baseColor) =>
            baseColor.IsDarkForTheEye() ? Colors.White : Colors.Black;

        public static Color ToGrayScale(this Color baseColor)
        {
            var avg = (baseColor.Red + baseColor.Blue + baseColor.Green) / 3;
            return Color.FromRgb(avg, avg, avg);
        }

        public static bool IsDarkForTheEye(this Color c) =>
            (c.GetByteRed() * 0.299) + (c.GetByteGreen() * 0.587) + (c.GetByteBlue() * 0.114) <= 186;

        public static bool IsDark(this Color c) => c.GetByteRed() + c.GetByteGreen() + c.GetByteBlue() <= 127 * 3;

        static byte ToByte(double input)
        {
            if (input < 0)
                return 0;
            if (input > 255)
                return 255;
            return (byte)Math.Round(input);
        }
    }
}