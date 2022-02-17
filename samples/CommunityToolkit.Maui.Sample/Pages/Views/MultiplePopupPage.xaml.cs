using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MultiplePopupPage : BasePage<MultiplePopupViewModel>
{
	public MultiplePopupPage(MultiplePopupViewModel multiplePopupViewModel) 
		: base(multiplePopupViewModel)
	{
		InitializeComponent();

		// Todo Put these views inside a page
		//SectionModel.Create<PopupGalleryViewModel>(typeof(SimplePopup), "Simple Popup", Colors.Red, "Displays a basic popup centered on the screen"),
		//	SectionModel.Create<PopupGalleryViewModel>(typeof(ButtonPopup), "Popup With 1 Button", Colors.Red, "Displays a basic popup with a confirm button"),
		//	SectionModel.Create<PopupGalleryViewModel>(typeof(MultipleButtonPopup), "Popup With Multiple Buttons", Colors.Red, "Displays a basic popup with a cancel and confirm button"),
		//	SectionModel.Create<PopupGalleryViewModel>(typeof(NoLightDismissPopup), "Simple Popup Without Light Dismiss", Colors.Red, "Displays a basic popup but does not allow the user to close it if they tap outside of the popup. In other words the LightDismiss is set to false."),
		//	SectionModel.Create<PopupGalleryViewModel>(typeof(ToggleSizePopup), "Toggle Size Popup", Colors.Red, "Displays a popup that can have it's size updated by pressing a button"),
		//	SectionModel.Create<PopupGalleryViewModel>(typeof(TransparentPopup), "Transparent Popup", Colors.Red, "Displays a popup with a transparent background"),
		//	SectionModel.Create<PopupGalleryViewModel>(typeof(OpenedEventSimplePopup), "Opened Event Popup", Colors.Red, "Popup with opened event"),
		//	SectionModel.Create<PopupGalleryViewModel>(typeof(ReturnResultPopup), "Return Result Popup", Colors.Red, "A popup that returns a string message when dismissed"),
		//	SectionModel.Create<XamlBindingPopupViewModel>(typeof(XamlBindingPopup), "Xaml Binding Popup", Colors.Red, "A simple popup that uses XAML BindingContext"),
		//	SectionModel.Create<CsharpBindingPopupViewModel>(typeof(CsharpBindingPopup), "C# Binding Popup", Colors.Red, "A simple popup that uses C# BindingContext")
	}

	void HandleSimplePopupButtonClicked(object sender, EventArgs e)
	{

	}
}