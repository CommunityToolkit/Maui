using System.Collections.Frozen;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ListToStringConverterViewModel : BaseViewModel
{
	public FrozenSet<string> ItemSource { get; } = new[]
	{
		"This",
		"Is",
		"The",
		"ListToStringConverter"
	}.ToFrozenSet();
}