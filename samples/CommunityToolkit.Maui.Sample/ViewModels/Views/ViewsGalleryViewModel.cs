using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed partial class ViewsGalleryViewModel() : BaseGalleryViewModel(
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
	SectionModel.Create<DrawingViewViewModel>("DrawingView", Colors.Red, "DrawingView provides a canvas for users to \"paint\" on the screen. The drawing can also be captured and displayed as an Image."),
	SectionModel.Create<ExpanderViewModel>("Expander Page", Colors.Red, "Expander allows collapse and expand content."),
	SectionModel.Create<BasicMapsViewModel>("Maps Basic Page", Colors.Red, "A page demonstrating a basic example of .NET MAUI Maps"),
	SectionModel.Create<LazyViewViewModel>("LazyView", Colors.Red, "LazyView is a view that allows you to load its children in a delayed manner."),
	SectionModel.Create<MapsPinsViewModel>("Maps Pins Page", Colors.Red, "A page demonstrating .NET MAUI Maps with Pins."),
	SectionModel.Create<MediaElementViewModel>("MediaElement", Colors.Red, "MediaElement is a view for playing video and audio"),
	SectionModel.Create<MediaElementFromStreamViewModel>("MediaElement from Stream", Colors.Red, "MediaElement can load media from a Stream source"),
    SectionModel.Create<MediaElementCarouselViewViewModel>("MediaElement in CarouselView", Colors.Red, "MediaElement can be used inside a DataTemplate in a CarouselView"),
	SectionModel.Create<MediaElementCollectionViewViewModel>("MediaElement in CollectionView", Colors.Red, "MediaElement can be used inside a DataTemplate in a CollectionView"),
	SectionModel.Create<MediaElementMultipleWindowsViewModel>("MediaElement in a Multi-Window Application", Colors.Red, "Demonstrates that MediaElement can be used inside a DataTemplate simultaneously on multiple windows"),
	SectionModel.Create<RatingViewShowcaseViewModel>("RatingView Showcase Page", Colors.Red, "A page with showcase examples for the RatingView control."),
	SectionModel.Create<RatingViewXamlViewModel>("RatingView XAML Page", Colors.Red, "A page demonstrating the RatingView control and possible uses using XAML"),
	SectionModel.Create<RatingViewCsharpViewModel>("RatingView C# Page", Colors.Red, "A page demonstrating the RatingView control and possible uses using C#"),
	SectionModel.Create<SemanticOrderViewPageViewModel>("Semantic Order View", Colors.Red, "SemanticOrderView allows developers to indicate the focus order of visible controls when a user is navigating via TalkBack (Android), VoiceOver (iOS) or Narrator (Windows)."),
	SectionModel.Create<PopupsViewModel>("Popups Page", Colors.Red, "A page demonstrating multiple different Popups")
]);