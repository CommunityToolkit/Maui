using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public partial class EnumToBoolConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	MyDevicePlatform platform;
	
	[ObservableProperty]
	ICollection<MyDevicePlatform> platforms;

	public EnumToBoolConverterViewModel()
	{
		platforms = new List<MyDevicePlatform>(Enum.GetValues<MyDevicePlatform>());
		Enum.TryParse(DeviceInfo.Platform.ToString(), true, out platform);
	}
}

public enum MyDevicePlatform
{
	Android,
	iOS,
	MacCatalyst,
	Tizen,
	WinUI,
}