using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class StateToBooleanConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial LayoutState LayoutState { get; set; } = LayoutState.None;

	[RelayCommand]
	public void ChangeLayout()
	{
		LayoutState = LayoutState switch
		{
			LayoutState.None => LayoutState.Success,
			_ => LayoutState.None
		};
	}
}