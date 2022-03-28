using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class MathExpressionConverterPage : BasePage<MathExpressionConverterViewModel>
{
	public MathExpressionConverterPage(IDeviceInfo deviceInfo, MathExpressionConverterViewModel mathExpressionConverterViewModel)
		: base(deviceInfo, mathExpressionConverterViewModel)
	{
		InitializeComponent();
	}
}