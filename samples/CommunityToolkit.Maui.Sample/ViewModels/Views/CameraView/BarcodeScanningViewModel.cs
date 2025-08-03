using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class BarcodeScanningViewModel() : BaseViewModel
{
	[ObservableProperty]
	public partial string DetectedCode { get; set; }
	
	[RelayCommand]
	void OnCodeDetected(string code)
	{
		DetectedCode = code;
	}
}