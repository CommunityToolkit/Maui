
namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ListToStringConverterViewModel : BaseViewModel
{
	public IReadOnlyList<string> ItemSource { get; } = new[]
	{
		"This",
		"Is",
		"The",
		"ListToStringConverter"
	};
}