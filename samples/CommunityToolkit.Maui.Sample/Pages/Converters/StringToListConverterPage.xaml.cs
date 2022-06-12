using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class StringToListConverterPage : BasePage<StringToListConverterViewModel>
{
	public StringToListConverterPage(StringToListConverterViewModel stringToListConverterViewModel)
		: base(stringToListConverterViewModel)
	{
		Resources.Add(nameof(StringToListConverter), new StringToListConverter
		{
			SplitOptions = StringSplitOptions.RemoveEmptyEntries,
			Separators = new[] { ",", ".", ";" }
		});

		InitializeComponent();
	}
}