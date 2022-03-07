using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class InvertedBoolConverterPage : BasePage<InvertedBoolConverterViewModel>
{
	public InvertedBoolConverterPage(InvertedBoolConverterViewModel invertedBoolConverterViewModel)
		: base(invertedBoolConverterViewModel)
	{
		InitializeComponent();
	}
}