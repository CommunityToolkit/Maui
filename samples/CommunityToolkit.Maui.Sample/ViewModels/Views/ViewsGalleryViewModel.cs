using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views.AvatarView;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed partial class ViewsGalleryViewModel() : BaseGalleryViewModel(
[
	SectionModel.Create<AvatarViewBindablePropertiesViewModel>("AvatarView Bindable Properties MainPage", Colors.Red, "A page demonstrating how to bind to various AvatarView properties."),
	SectionModel.Create<AvatarViewBordersViewModel>("AvatarView Borders MainPage", Colors.Red, "A page demonstrating AvatarView borders."),
	SectionModel.Create<AvatarViewColorsViewModel>("AvatarView Colors MainPage", Colors.Red, "A page demonstrating AvatarViews with different color options."),
	SectionModel.Create<AvatarViewDayOfWeekViewModel>("AvatarView Day of Week MainPage", Colors.Red, "A page demonstrating AvatarViews as a Day of the Week."),
	SectionModel.Create<AvatarViewGesturesViewModel>("AvatarView Gestures MainPage", Colors.Red, "A page demonstrating AvatarViews with different gesture options."),
	SectionModel.Create<AvatarViewImagesViewModel>("AvatarView Images MainPage", Colors.Red, "A page demonstrating AvatarViews with different image options."),
	SectionModel.Create<AvatarViewKeyboardViewModel>("AvatarView Keyboard MainPage", Colors.Red, "A page demonstrating AvatarViews aligned as a Keyboard."),
	SectionModel.Create<AvatarViewRatingViewModel>("AvatarView Rating MainPage", Colors.Red, "A page demonstrating AvatarViews as a star rating."),
	SectionModel.Create<AvatarViewShadowsViewModel>("AvatarView Shadows MainPage", Colors.Red, "A page demonstrating AvatarViews with various shadow options."),
	SectionModel.Create<AvatarViewShapesViewModel>("AvatarView Shapes MainPage", Colors.Red, "A page demonstrating AvatarViews with various shape options."),
	SectionModel.Create<AvatarViewSizesViewModel>("AvatarView Sizes MainPage", Colors.Red, "A page demonstrating AvatarViews with various size options."),
	SectionModel.Create<CameraViewViewModel>("CameraView MainPage", Colors.Red, "CameraView is a view for displaying camera output."),
	SectionModel.Create<CustomSizeAndPositionPopupViewModel>("Custom Size and Positioning Popup", Colors.Red, "Displays a basic popup anywhere on the screen with a custom size using VerticalOptions and HorizontalOptions."),
	SectionModel.Create<DrawingViewViewModel>("DrawingView", Colors.Red, "DrawingView provides a canvas for users to \"paint\" on the screen. The drawing can also be captured and displayed as an Image."),
	SectionModel.Create<ExpanderViewModel>("Expander MainPage", Colors.Red, "Expander allows collapse and expand content."),
	SectionModel.Create<BasicMapsViewModel>("Windows Maps Basic MainPage", Colors.Red, "A page demonstrating a basic example of .NET MAUI Maps for Windows."),
	SectionModel.Create<LazyViewViewModel>("LazyView", Colors.Red, "LazyView is a view that allows you to load its children in a delayed manner."),
	SectionModel.Create<MapsPinsViewModel>("Windows Maps Pins MainPage", Colors.Red, "A page demonstrating .NET MAUI Maps for Windows with Pins."),
	SectionModel.Create<MediaElementViewModel>("MediaElement", Colors.Red, "MediaElement is a view for playing video and audio"),
	SectionModel.Create<MediaElementCarouselViewViewModel>("MediaElement in CarouselView", Colors.Red, "MediaElement can be used inside a DataTemplate in a CarouselView"),
	SectionModel.Create<MediaElementCollectionViewViewModel>("MediaElement in CollectionView", Colors.Red, "MediaElement can be used inside a DataTemplate in a CollectionView"),
	SectionModel.Create<MediaElementMultipleWindowsViewModel>("MediaElement in a Multi-Window Application", Colors.Red, "Demonstrates that MediaElement can be used inside a DataTemplate simultaneously on multiple windows"),
	SectionModel.Create<MultiplePopupViewModel>("Multiple Popups MainPage", Colors.Red, "A page demonstrating multiple different Popups"),
	SectionModel.Create<PopupPositionViewModel>("Custom Positioning Popup", Colors.Red, "Displays a basic popup anywhere on the screen using VerticalOptions and HorizontalOptions"),
	SectionModel.Create<PopupAnchorViewModel>("Anchor Popup", Colors.Red, "Popups can be anchored to other view's on the screen"),
	SectionModel.Create<PopupLayoutAlignmentViewModel>("Popup Layout MainPage", Colors.Red, "Popup.Content demonstrated using different layouts"),
	SectionModel.Create<PopupSizingIssuesViewModel>("Popup Sizing Issues MainPage", Colors.Red, "A page demonstrating how Popups can be styled in a .NET MAUI application."),
	SectionModel.Create<ShowPopupInOnAppearingPageViewModel>("Show Popup in OnAppearing", Colors.Red, "Proves that we now support showing a popup before the platform is even ready."),
	SectionModel.Create<SemanticOrderViewPageViewModel>("Semantic Order View", Colors.Red, "SemanticOrderView allows developers to indicate the focus order of visible controls when a user is navigating via TalkBack (Android), VoiceOver (iOS) or Narrator (Windows)."),
	SectionModel.Create<StylePopupViewModel>("Popup Style MainPage", Colors.Red, "A page demonstrating how Popups can be styled in a .NET MAUI application.")
]);