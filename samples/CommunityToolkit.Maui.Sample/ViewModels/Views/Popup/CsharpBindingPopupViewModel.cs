using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed partial class CsharpBindingPopupViewModel : BaseViewModel, IQueryAttributable
{
	[ObservableProperty]
	public partial string Title { get; private set; } = string.Empty;

	[ObservableProperty]
	public partial string Message { get; private set; } = string.Empty;

	public TaskCompletionSource<IPopupResult>? PopupResultManager { get; set; }

	void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
	{
		Title = (string)query[nameof(Title)];
		Message = (string)query[nameof(Message)];
	}
}