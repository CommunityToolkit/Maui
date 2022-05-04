using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class TextCaseConverterPage : BasePage<TextCaseConverterViewModel>
{
	public TextCaseConverterPage(TextCaseConverterViewModel textCaseConverterViewModel)
		: base(textCaseConverterViewModel)
	{
		InitializeComponent();
	}
}