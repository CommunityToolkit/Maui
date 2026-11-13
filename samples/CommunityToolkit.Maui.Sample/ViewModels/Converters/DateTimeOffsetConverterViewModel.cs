using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class DateTimeOffsetConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial DateTimeOffset TheDate { get; set; } = DateTimeOffset.Now;
}