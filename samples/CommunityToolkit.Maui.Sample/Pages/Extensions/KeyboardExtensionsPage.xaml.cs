using CommunityToolkit.Maui.Sample.ViewModels.Extensions;

namespace CommunityToolkit.Maui.Sample.Pages.Extensions;

public partial class KeyboardExtensionsPage : BasePage<KeyboardExtensionsViewModel>
{
	public KeyboardExtensionsPage(KeyboardExtensionsViewModel viewModel) :
		base(viewModel)
	{
		InitializeComponent();
	}

	void OnEntryFocused(System.Object sender, Microsoft.Maui.Controls.FocusEventArgs e)
	{
#if IOS || MACCATALYST
		// Currently .NET MAUI will auto close the keyboard on iOS if you click outside of the entry
		// This causes the examples on this page to behave oddly, because the keybord auto closes if the user
		// taps the "hide keyboard" or "check keyboard status" buttons.
		// We're going to remove this as the default behavior in .NET 8 and then users can re-enable it via
		// https://github.com/CommunityToolkit/Maui/issues/978
		// This code removes the TapGestureRecognizer used to detect and close the keyboard so that we can accurately
		// validate that these APIs work on iOS
		if (this?.Handler?.PlatformView is UIKit.UIView uiView &&
			uiView.GestureRecognizers is not null)
		{
			foreach (var gesture in uiView.GestureRecognizers)
			{
				if (gesture is UIKit.UITapGestureRecognizer gr)
				{
					uiView.RemoveGestureRecognizer(gr);
					break;
				}
			}
		}
#endif
	}
}