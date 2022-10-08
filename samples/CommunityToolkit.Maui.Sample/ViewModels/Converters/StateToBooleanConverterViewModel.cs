using System.Windows.Input;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;
public partial class StateToBooleanConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	LayoutState layoutState = LayoutState.None;

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