using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;
public partial class IsInRangeConverterPage : BasePage<IsInRangeConverterViewModel>
{
	public IsInRangeConverterPage(IsInRangeConverterViewModel viewModel)
		: base(viewModel)
	{
		InitializeComponent();

		//IsInRangeConverter isInRangeConverterText = new()
		//{
		//	TrueObject = "True",
		//	FalseObject = "False",
		//	Min = 'G',
		//	Max = 'L'
		//};

		//LabelCharInRange?.SetBinding(Label.TextProperty, new Binding(CharText.Text, BindingMode.OneWay, isInRangeConverterText));
	}
}