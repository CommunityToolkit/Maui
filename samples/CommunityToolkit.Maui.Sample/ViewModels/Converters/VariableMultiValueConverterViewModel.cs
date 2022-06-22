using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class VariableMultiValueConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	bool isAllGroupSwitch1On, isAllGroupSwitch2On, isAnyGroupSwitch1On, isAnyGroupSwitch2On,
		isGreaterThanGroupSwitch1On, isGreaterThanGroupSwitch2On, isGreaterThanGroupSwitch3On, isGreaterThanGroupSwitch4On;
}