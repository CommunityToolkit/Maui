
namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class ListToStringConverterViewModel : BaseViewModel
{
	public IReadOnlyList<string> ItemSource { get; } =
	[
		"This",
		"Is",
		"The",
		"ListToStringConverter"
	];
}