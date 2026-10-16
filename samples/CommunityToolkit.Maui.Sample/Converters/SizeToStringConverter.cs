using System.Globalization;
using CommunityToolkit.Maui.Converters;

namespace CommunityToolkit.Maui.Sample.Converters;

/// <summary>
/// Converts a <see cref="Size"/> to its width and height, for example "1280 x 720".
/// </summary>
[AcceptEmptyServiceProvider]
public sealed partial class SizeToStringConverter : BaseConverterOneWay<Size, string>
{
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	public override string ConvertFrom(Size value, CultureInfo? culture) => $"{value.Width} x {value.Height}";
}