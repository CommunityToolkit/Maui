using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Extensions;
public partial class KeyboardExtensionsViewModel : BaseViewModel
{

	public KeyboardExtensionsViewModel()
	{
		ShowKeyboard = new Command<ITextInput>(OnShowKeyboard);
		HideKeyboard = new Command<ITextInput>(OnHideKeyboard);
		IsKeyboardShowing = new Command<ITextInput>(OnIsKeyboardShowing);
	}

	void OnIsKeyboardShowing(ITextInput view)
	{
		if (!view.IsSoftKeyboardShowing())
		{
			KeyboardShowingText = $"Soft Input Is Currently Hidden";
		}
		else
		{
			KeyboardShowingText = $"Soft Input Is Currently Showing";
		}
	}

	async void OnHideKeyboard(ITextInput view)
	{
		if (await view.HideKeyboardAsync())
		{
			OperationResult = "Hide Succeeded";
		}
		else
		{
			OperationResult = "Hide Failed";
		}
	}

	async void OnShowKeyboard(ITextInput view)
	{
		if (await view.ShowKeyboardAsync())
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

	[ObservableProperty]
	Command<ITextInput>? showKeyboard;

	[ObservableProperty]
	Command<ITextInput>? hideKeyboard;

	[ObservableProperty]
	Command<ITextInput>? isKeyboardShowing;
}