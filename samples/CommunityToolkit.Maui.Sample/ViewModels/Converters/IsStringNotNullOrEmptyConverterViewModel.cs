using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class IsStringNotNullOrEmptyConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string? LabelText { get; set; }
}