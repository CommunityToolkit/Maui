using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Extensions;

public partial class KeyboardExtensionsViewModel : BaseViewModel
{
	[ObservableProperty]
	string operationResult = string.Empty;

	[RelayCommand]
	void OnIsKeyboardShowing(ITextInput view)
	{
		if (view.IsSoftKeyboardShowing())
		{
			OperationResult = $"Soft Input Is Currently Showing";
		}
		else
		{
			OperationResult = $"Soft Input Is Currently Hidden";
		}
	}

	[RelayCommand]
	async Task OnHideKeyboard(ITextInput view, CancellationToken token)
	{
		try
		{
			bool isSuccessful = await view.HideKeyboardAsync(token);

			if (isSuccessful)
			{
				OperationResult = "Hide Succeeded";
			}
			else
			{
				OperationResult = "Hide Failed";
			}
		}
		catch (Exception e)
		{
			OperationResult = e.Message;
		}
	}

	[RelayCommand]
	async Task OnShowKeyboard(ITextInput view, CancellationToken token)
	{
		try
		{
			bool isSuccessful = await view.ShowKeyboardAsync(token);

			if (isSuccessful)
			{
				OperationResult = "Show Succeeded";
			}
			else
			{
				OperationResult = "Show Failed";
			}
		}
		catch (Exception e)
		{
			OperationResult = e.Message;
		}
	}
}