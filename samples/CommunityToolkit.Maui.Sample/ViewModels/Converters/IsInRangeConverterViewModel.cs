namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class IsInRangeConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(InputChar))]
	public partial string InputString { get; set; } = "H";

	[ObservableProperty]
	public partial TimeSpan InputTime { get; set; } = new(12, 0, 0);

	public char InputChar => char.TryParse(InputString, out var returnChar) ? returnChar : default;
}