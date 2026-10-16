using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class EdgeToEdgeShellViewModel : BaseViewModel
{
	public Color StatusBarColor => IsSolidStatusBarColor ? Colors.Red : Colors.Transparent;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StatusBarColor))]
	public partial bool IsSolidStatusBarColor { get; set; }
}