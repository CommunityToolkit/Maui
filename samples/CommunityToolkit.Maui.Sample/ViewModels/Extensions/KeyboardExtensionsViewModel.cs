using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Extensions;

public partial class KeyboardExtensionsViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial string OperationResult { get; set; } = string.Empty;

	[RelayCommand]
	void OnIsKeyboardShowing(ITextInput view)
	{
		try
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
		catch (Exception e)
		{
			// Mac Catalyst has no soft keyboard: IsSoftKeyboardShowing throws a NotSupportedException there, like ShowKeyboardAsync and HideKeyboardAsync
			OperationResult = e.Message;
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