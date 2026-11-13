using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class IsNotNullConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial int IntCheck { get; set; }

	[ObservableProperty]
	public partial List<string>? ListCheck { get; set; }

	[ObservableProperty]
	public partial string? StringCheck { get; set; }

	[ObservableProperty]
	public partial object? ObjectCheck { get; set; }
}