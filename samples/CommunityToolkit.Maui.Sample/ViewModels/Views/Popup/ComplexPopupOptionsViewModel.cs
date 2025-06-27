using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class ComplexPopupOptionsViewModel : ObservableObject
{
	[ObservableProperty]
	public partial Color PageOverlayBackgroundColor { get; set; } = Colors.Orange.WithAlpha(0.2f);
}
