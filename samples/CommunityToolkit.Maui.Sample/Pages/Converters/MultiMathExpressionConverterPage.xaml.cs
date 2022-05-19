using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class MultiMathExpressionConverterPage : BasePage<MultiMathExpressionConverterViewModel>
{
	public MultiMathExpressionConverterPage(MultiMathExpressionConverterViewModel mathExpressionConverterViewModel)
		: base(mathExpressionConverterViewModel)
	{
		InitializeComponent();
	}
}