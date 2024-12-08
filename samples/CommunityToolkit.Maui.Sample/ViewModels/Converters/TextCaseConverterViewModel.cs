using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class TextCaseConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string Input { get; set; } = string.Empty;
}