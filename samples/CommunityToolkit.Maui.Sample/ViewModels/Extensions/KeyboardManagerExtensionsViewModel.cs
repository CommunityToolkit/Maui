using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core.Platform;

namespace CommunityToolkit.Maui.Sample.ViewModels.Extensions;
public class KeyboardManagerExtensionsViewModel : BaseViewModel
{
	public Command<ITextInput> ShowKeyboard { get; }
	public Command<ITextInput> HideKeyboard { get; }

	public KeyboardManagerExtensionsViewModel()
	{
		ShowKeyboard = new Command<ITextInput>(OnShowKeyboard);
		HideKeyboard = new Command<ITextInput>(OnHideKeyboard);
	}

	void OnHideKeyboard(ITextInput view)
	{
		view.HideKeyboard();
	}

	void OnShowKeyboard(ITextInput view)
	{
		view.ShowKeyboard();
	}
}
