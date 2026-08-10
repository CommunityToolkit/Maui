using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui;

static class TextValidationBehaviorDefaults
{
	public const int MinimumLength = 0;
	public const int MaximumLength = int.MaxValue;
	public const TextDecorationFlags DecorationFlags = TextDecorationFlags.None;
	public const string RegexPattern = "";
	public const RegexOptions RegexOptions = System.Text.RegularExpressions.RegexOptions.None;
}