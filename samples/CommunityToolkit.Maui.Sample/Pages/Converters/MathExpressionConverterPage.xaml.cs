using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class MathExpressionConverterPage : BasePage<MathExpressionConverterViewModel>
{
	public MathExpressionConverterPage(MathExpressionConverterViewModel mathExpressionConverterViewModel)
		: base(mathExpressionConverterViewModel)
	{
		InitializeComponent();
	}
}