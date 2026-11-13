using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed partial class ViewsGalleryViewModel() : BaseGalleryViewModel(
[
	SectionModel.Create<AvatarViewsViewModel>("AvatarViews Page", Colors.Red, "A page demonstrating multiple different AvatarViews"),
	SectionModel.Create<CameraViewViewModel>("CameraView Page", Colors.Red, "CameraView is a view for displaying camera output."),
	SectionModel.Create<DrawingViewViewModel>("DrawingView", Colors.Red, "DrawingView provides a canvas for users to \"paint\" on the screen. The drawing can also be captured and displayed as an Image."),
	SectionModel.Create<ExpanderViewModel>("Expander Page", Colors.Red, "Expander allows collapse and expand content."),
	SectionModel.Create<LazyViewViewModel>("LazyView", Colors.Red, "LazyView is a view that allows you to load its children in a delayed manner."),
	SectionModel.Create<MediaElementsViewModel>("MediaElements Page", Colors.Red, "A page demonstrating multiple different MediaElements"),
	SectionModel.Create<RangeSliderViewModel>("RangeSlider Page", Colors.Red, "A page demonstrating RangeSlider in various styles."),
	SectionModel.Create<RatingViewsViewModel>("RatingViews Page", Colors.Red, "A page demonstrating multiple different RatingViews"),
	SectionModel.Create<SemanticOrderViewPageViewModel>("Semantic Order View", Colors.Red, "SemanticOrderView allows developers to indicate the focus order of visible controls when a user is navigating via TalkBack (Android), VoiceOver (iOS) or the Tab key (Windows)."),
	SectionModel.Create<PopupsViewModel>("Popups Page", Colors.Red, "A page demonstrating multiple different Popups")
]);