using CommunityToolkit.Maui.Sample.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample;

public sealed partial class DynamicSizeContentPopupViewModel : BaseViewModel
{
	[ObservableProperty]
	bool toggle = false;

	[RelayCommand]
	public void ToggleBool() => Toggle = !Toggle;
}