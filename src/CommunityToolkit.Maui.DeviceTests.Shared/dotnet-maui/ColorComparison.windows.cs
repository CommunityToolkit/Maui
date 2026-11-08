// Vendored from dotnet/maui @ 2067dd42c6032fbc40d2e46245806679a85df5e6
// Original path: src/TestUtils/src/DeviceTests/ColorComparison.Windows.cs
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License (see LICENSE-dotnet-maui.txt).
// Toolkit change: namespace changed from Microsoft.Maui(.DeviceTests) to CommunityToolkit.Maui.DeviceTests.
// Toolkit change: renamed to the toolkit's platform suffix (ColorComparison.windows.cs).
// Toolkit change: #nullable enable added: the upstream project enables nullable reference types, these copies are compiled as generated code.
#nullable enable
using WColor = Windows.UI.Color;

namespace CommunityToolkit.Maui.DeviceTests
{
	public static class ColorComparison
	{
		public const int ColorPrecision = 1;

		public static bool IsEquivalent(this WColor actualColor, WColor expectedColor, int precision = ColorPrecision)
		{
			var red = actualColor.R <= expectedColor.R + precision && actualColor.R >= expectedColor.R - precision;
			var green = actualColor.G <= expectedColor.G + precision && actualColor.G >= expectedColor.G - precision;
			var blue = actualColor.B <= expectedColor.B + precision && actualColor.B >= expectedColor.B - precision;
			var alpha = actualColor.A <= expectedColor.A + precision && actualColor.A >= expectedColor.A - precision;

			return red && green && blue && alpha;
		}
	}
}