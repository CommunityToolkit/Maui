using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class TextCaseConverterPage : BasePage
{
	public TextCaseConverterPage()
	{
		InitializeComponent();

		ExampleText ??= new();
	}
}