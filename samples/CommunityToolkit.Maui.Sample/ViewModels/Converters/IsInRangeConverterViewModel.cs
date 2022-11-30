namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class IsInRangeConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(InputChar))]
	string inputString = "H";

	public char InputChar => char.TryParse(InputString, out var returnChar) ? returnChar : default;
}