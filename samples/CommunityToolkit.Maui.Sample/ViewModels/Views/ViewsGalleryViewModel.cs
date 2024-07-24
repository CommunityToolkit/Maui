using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views.AvatarView;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed class ViewsGalleryViewModel() : BaseGalleryViewModel(
[
	SectionModel.Create<AvatarViewBindablePropertiesViewModel>("AvatarView Bindable Properties Page", Colors.Red, "A page demonstrating how to bind to various AvatarView properties."),
	SectionModel.Create<AvatarViewBordersViewModel>("AvatarView Borders Page", Colors.Red, "A page demonstrating AvatarView borders."),
	SectionModel.Create<AvatarViewColorsViewModel>("AvatarView Colors Page", Colors.Red, "A page demonstrating AvatarViews with different color options."),
	SectionModel.Create<AvatarViewDayOfWeekViewModel>("AvatarView Day of Week Page", Colors.Red, "A page demonstrating AvatarViews as a Day of the Week."),
	SectionModel.Create<AvatarViewGesturesViewModel>("AvatarView Gestures Page", Colors.Red, "A page demonstrating AvatarViews with different gesture options."),
	SectionModel.Create<AvatarViewImagesViewModel>("AvatarView Images Page", Colors.Red, "A page demonstrating AvatarViews with different image options."),
	SectionModel.Create<AvatarViewKeyboardViewModel>("AvatarView Keyboard Page", Colors.Red, "A page demonstrating AvatarViews aligned as a Keyboard."),
	SectionModel.Create<AvatarViewRatingViewModel>("AvatarView Rating Page", Colors.Red, "A page demonstrating AvatarViews as a star rating."),
	SectionModel.Create<AvatarViewShadowsViewModel>("AvatarView Shadows Page", Colors.Red, "A page demonstrating AvatarViews with various shadow options."),
	SectionModel.Create<AvatarViewShapesViewModel>("AvatarView Shapes Page", Colors.Red, "A page demonstrating AvatarViews with various shape options."),
	SectionModel.Create<AvatarViewSizesViewModel>("AvatarView Sizes Page", Colors.Red, "A page demonstrating AvatarViews with various size options."),
	SectionModel.Create<CameraViewViewModel>("CameraView Page", Colors.Red, "CameraView is a view for displaying camera output."),
	SectionModel.Create<CustomSizeAndPositionPopupViewModel>("Custom Size and Positioning Popup", Colors.Red, "Displays a basic popup anywhere on the screen with a custom size using VerticalOptions and HorizontalOptions."),
	SectionModel.Create<DrawingViewViewModel>("DrawingView", Colors.Red, "DrawingView provides a canvas for users to \"paint\" on the screen. The drawing can also be captured and displayed as an Image."),
	SectionModel.Create<ExpanderViewModel>("Expander Page", Colors.Red, "Expander allows collapse and expand content."),
	SectionModel.Create<BasicMapsViewModel>("Windows Maps Basic Page", Colors.Red, "A page demonstrating a basic example of .NET MAUI Maps for Windows."),
	SectionModel.Create<MapsPinsViewModel>("Windows Maps Pins Page", Colors.Red, "A page demonstrating .NET MAUI Maps for Windows with Pins."),
	SectionModel.Create<LazyViewViewModel>("LazyView", Colors.Red, "LazyView is a view that allows you to load its children in a delayed manner."),
	SectionModel.Create<MediaElementViewModel>("MediaElement", Colors.Red, "MediaElement is a view for playing video and audio"),
	SectionModel.Create<MultiplePopupViewModel>("Multiple Popups Page", Colors.Red, "A page demonstrating multiple different Popups"),
	SectionModel.Create<PopupPositionViewModel>("Custom Positioning Popup", Colors.Red, "Displays a basic popup anywhere on the screen using VerticalOptions and HorizontalOptions"),
	SectionModel.Create<PopupAnchorViewModel>("Anchor Popup", Colors.Red, "Popups can be anchored to other view's on the screen"),
	SectionModel.Create<PopupLayoutAlignmentViewModel>("Popup Layout Page", Colors.Red, "Popup.Content demonstrated using different layouts"),
	SectionModel.Create<PopupSizingIssuesViewModel>("Popup Sizing Issues Page", Colors.Red, "A page demonstrating how Popups can be styled in a .NET MAUI application."),
	SectionModel.Create<ShowPopupInOnAppearingPageViewModel>("Show Popup in OnAppearing", Colors.Red, "Proves that we now support showing a popup before the platform is even ready."),
	SectionModel.Create<SemanticOrderViewPageViewModel>("Semantic Order View", Colors.Red, "SemanticOrderView allows developers to indicate the focus order of visible controls when a user is navigating via TalkBack (Android), VoiceOver (iOS) or Narrator (Windows)."),
	SectionModel.Create<StylePopupViewModel>("Popup Style Page", Colors.Red, "A page demonstrating how Popups can be styled in a .NET MAUI application.")
]);