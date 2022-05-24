using System.Windows.Input;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;
public partial class StateToBooleanConverterViewModel : BaseViewModel
{
	[ObservableProperty]
	LayoutState layoutState = LayoutState.None;

	public ICommand ChangeLayoutCommand { get; }

	public StateToBooleanConverterViewModel()
	{
		ChangeLayoutCommand = new Command(ChangeLayout);
	}

	public void ChangeLayout()
	{
		LayoutState = LayoutState == LayoutState.None ? LayoutState.Success : LayoutState.None;
	}
}