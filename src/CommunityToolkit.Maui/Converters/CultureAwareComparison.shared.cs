using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

static class CultureAwareComparison
{
	// string.CompareTo always collates with CurrentCulture, but a binding's culture must decide string order. A null culture keeps the CurrentCulture behavior that direct callers had.
	internal static int Compare(IComparable value, object? other, CultureInfo? culture) =>
		culture is not null && value is string text && other is string otherText
			? culture.CompareInfo.Compare(text, otherText, CompareOptions.None)
			: value.CompareTo(other);
}
