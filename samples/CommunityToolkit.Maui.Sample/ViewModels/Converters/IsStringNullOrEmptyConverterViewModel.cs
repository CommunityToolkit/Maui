using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class IsStringNullOrEmptyConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string? LabelText { get; set; }
}