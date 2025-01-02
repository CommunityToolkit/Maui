using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class VariableMultiValueConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial bool IsAllGroupSwitch1On { get; set; }

	[ObservableProperty]
	public partial bool IsAllGroupSwitch2On { get; set; }

	[ObservableProperty]
	public partial bool IsAnyGroupSwitch1On { get; set; }

	[ObservableProperty]
	public partial bool IsAnyGroupSwitch2On { get; set; }

	[ObservableProperty]
	public partial bool IsGreaterThanGroupSwitch1On { get; set; }

	[ObservableProperty]
	public partial bool IsGreaterThanGroupSwitch2On { get; set; }

	[ObservableProperty]
	public partial bool IsGreaterThanGroupSwitch3On { get; set; }

	[ObservableProperty]
	public partial bool IsGreaterThanGroupSwitch4On { get; set; }
}