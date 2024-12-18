using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class EnumToBoolConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial MyDevicePlatform SelectedPlatform { get; set; } = Enum.Parse<MyDevicePlatform>(DeviceInfo.Platform.ToString(), true);

	public IReadOnlyCollection<MyDevicePlatform> Platforms { get; } = Enum.GetValues<MyDevicePlatform>();
}

public enum MyDevicePlatform
{
	Android,
	iOS,
	MacCatalyst,
	Tizen,
	WinUI,
}