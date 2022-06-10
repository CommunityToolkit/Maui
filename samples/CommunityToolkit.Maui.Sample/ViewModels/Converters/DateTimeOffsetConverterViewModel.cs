using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class DateTimeOffsetConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	DateTimeOffset theDate = DateTimeOffset.Now;
}