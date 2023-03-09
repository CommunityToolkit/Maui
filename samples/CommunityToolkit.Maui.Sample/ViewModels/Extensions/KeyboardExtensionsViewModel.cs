using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Extensions;
public partial class KeyboardExtensionsViewModel : BaseViewModel
{

	public KeyboardExtensionsViewModel()
	{
	}

	[RelayCommand]
	void OnIsKeyboardShowing(ITextInput view)
	{
		if (view.IsSoftKeyboardShowing())
		{
			KeyboardShowingText = $"Soft Input Is Currently Showing";
		}
		else
		{
			KeyboardShowingText = $"Soft Input Is Currently Hidden";
		}
	}

	[RelayCommand]
	async Task OnHideKeyboard(ITextInput view)
	{
		if (await view.HideKeyboardAsync(CancellationToken.None))
		{
			OperationResult = "Hide Succeeded";
		}
		else
		{
			OperationResult = "Hide Failed";
		}
	}

	[RelayCommand]
	async Task OnShowKeyboard(ITextInput view)
	{
		if (await view.ShowKeyboardAsync(CancellationToken.None))
		{
			OperationResult = "Show Succeeded";
		}
		else
		{
			OperationResult = "Show Failed";
		}
	}

	[ObservableProperty]
	string? keyboardShowingText;

	[ObservableProperty]
	string? operationResult;
}