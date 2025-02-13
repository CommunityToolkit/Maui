using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed partial class PopupContentViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string Message { get; private set; } = "";

	internal void SetMessage(string text) => this.Message = text;
}