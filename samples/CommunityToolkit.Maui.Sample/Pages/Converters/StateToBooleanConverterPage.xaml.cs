using CommunityToolkit.Maui.Sample.Pages;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample;

public partial class StateToBooleanConverterPage : BasePage<StateToBooleanConverterViewModel>
{
	public StateToBooleanConverterPage(StateToBooleanConverterViewModel stateToBooleanConverterViewModel)
		: base(stateToBooleanConverterViewModel)
	{
		InitializeComponent();
	}
}