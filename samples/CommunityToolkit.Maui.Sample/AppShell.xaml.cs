using CommunityToolkit.Maui.Sample.Pages;
using CommunityToolkit.Maui.Sample.Pages.Alerts;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;
using CommunityToolkit.Maui.Sample.Pages.Converters;
using CommunityToolkit.Maui.Sample.Pages.Essentials;
using CommunityToolkit.Maui.Sample.Pages.Extensions;
using CommunityToolkit.Maui.Sample.Pages.ImageSources;
using CommunityToolkit.Maui.Sample.Pages.Layouts;
using CommunityToolkit.Maui.Sample.Pages.Views;
using CommunityToolkit.Maui.Sample.ViewModels;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.Essentials;
using CommunityToolkit.Maui.Sample.ViewModels.ImageSources;
using CommunityToolkit.Maui.Sample.ViewModels.Layouts;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Sample.ViewModels.Views.AvatarView;

namespace CommunityToolkit.Maui.Sample;

public partial class AppShell : Shell
{
	static readonly IReadOnlyDictionary<Type, (Type GalleryPageType, Type ContentPageType)> viewModelMappings = new Dictionary<Type, (Type, Type)>(new[]
	{
		// Add Alerts View Models
		CreateViewModelMapping<SnackbarPage, SnackbarViewModel, AlertsGalleryPage, AlertsGalleryViewModel>(),
		CreateViewModelMapping<ToastPage, ToastViewModel, AlertsGalleryPage, AlertsGalleryViewModel>(),

		// Add Behaviors View Models
		CreateViewModelMapping<AnimationBehaviorPage, AnimationBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<CharactersValidationBehaviorPage, CharactersValidationBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<EmailValidationBehaviorPage, EmailValidationBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<EventToCommandBehaviorPage, EventToCommandBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<IconTintColorBehaviorPage, IconTintColorBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<MaskedBehaviorPage, MaskedBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<MaxLengthReachedBehaviorPage, MaxLengthReachedBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<MultiValidationBehaviorPage, MultiValidationBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<NumericValidationBehaviorPage, NumericValidationBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<ProgressBarAnimationBehaviorPage, ProgressBarAnimationBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<RequiredStringValidationBehaviorPage, RequiredStringValidationBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<SelectAllTextBehaviorPage, SelectAllTextBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<SetFocusOnEntryCompletedBehaviorPage, SetFocusOnEntryCompletedBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<StatusBarBehaviorPage, StatusBarBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<TextValidationBehaviorPage, TextValidationBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<UriValidationBehaviorPage, UriValidationBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<UserStoppedTypingBehaviorPage, UserStoppedTypingBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),

		// Add Converters View Models
		CreateViewModelMapping<BoolToObjectConverterPage, BoolToObjectConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<ByteArrayToImageSourceConverterPage, ByteArrayToImageSourceConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<ColorsConverterPage, ColorsConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<CompareConverterPage, CompareConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<DateTimeOffsetConverterPage, DateTimeOffsetConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<DoubleToIntConverterPage, DoubleToIntConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<EnumToBoolConverterPage, EnumToBoolConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<EnumToIntConverterPage, EnumToIntConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<ImageResourceConverterPage, ImageResourceConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<IndexToArrayItemConverterPage, IndexToArrayItemConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<IntToBoolConverterPage, IntToBoolConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<InvertedBoolConverterPage, InvertedBoolConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<IsEqualConverterPage, IsEqualConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<IsInRangeConverterPage, IsInRangeConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<IsListNotNullOrEmptyConverterPage, IsListNotNullOrEmptyConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<IsListNullOrEmptyConverterPage, IsListNullOrEmptyConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<IsNotEqualConverterPage, IsNotEqualConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<IsNotNullConverterPage, IsNotNullConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<IsNullConverterPage, IsNullConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<IsStringNotNullOrEmptyConverterPage, IsStringNotNullOrEmptyConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<IsStringNotNullOrWhiteSpaceConverterPage, IsStringNotNullOrWhiteSpaceConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<IsStringNullOrEmptyConverterPage, IsStringNullOrEmptyConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<IsStringNullOrWhiteSpaceConverterPage, IsStringNullOrWhiteSpaceConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<ItemTappedEventArgsConverterPage, ItemTappedEventArgsConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<ListToStringConverterPage, ListToStringConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<MathExpressionConverterPage, MathExpressionConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<MultiConverterPage, MultiConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<MultiMathExpressionConverterPage, MultiMathExpressionConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<SelectedItemEventArgsConverterPage, SelectedItemEventArgsConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<StateToBooleanConverterPage, StateToBooleanConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<StringToListConverterPage, StringToListConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<TextCaseConverterPage, TextCaseConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),
		CreateViewModelMapping<VariableMultiValueConverterPage, VariableMultiValueConverterViewModel, ConvertersGalleryPage, ConvertersGalleryViewModel>(),

		// Add Essentials View Models
		CreateViewModelMapping<FileSaverPage, FileSaverViewModel, EssentialsGalleryPage, EssentialsGalleryViewModel>(),
		CreateViewModelMapping<FolderPickerPage, FolderPickerViewModel, EssentialsGalleryPage, EssentialsGalleryViewModel>(),

		// Add Extensions View Models
		CreateViewModelMapping<ColorAnimationExtensionsPage, ColorAnimationExtensionsViewModel, ExtensionsGalleryPage, ExtensionsGalleryViewModel>(),

		// Add ImageSources View Models
		CreateViewModelMapping<GravatarImageSourcePage, GravatarImageSourceViewModel, ImageSourcesGalleryPage, ImageSourcesGalleryViewModel>(),

		// Add Layouts View Models
		CreateViewModelMapping<DockLayoutPage, DockLayoutViewModel, LayoutsGalleryPage, LayoutsGalleryViewModel>(),
		CreateViewModelMapping<StateContainerPage, StateContainerViewModel, LayoutsGalleryPage, LayoutsGalleryViewModel>(),
		CreateViewModelMapping<UniformItemsLayoutPage, UniformItemsLayoutViewModel, LayoutsGalleryPage, LayoutsGalleryViewModel>(),

		// Add Views View Models
		CreateViewModelMapping<AvatarViewBindablePropertiesPage, AvatarViewBindablePropertiesViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<AvatarViewBordersPage, AvatarViewBordersViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<AvatarViewColorsPage, AvatarViewColorsViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<AvatarViewDayOfWeekPage, AvatarViewDayOfWeekViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<AvatarViewGesturesPage, AvatarViewGesturesViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<AvatarViewImagesPage, AvatarViewImagesViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<AvatarViewKeyboardPage, AvatarViewKeyboardViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<AvatarViewRatingPage, AvatarViewRatingViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<AvatarViewShadowsPage, AvatarViewShadowsViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<AvatarViewShapesPage, AvatarViewShapesViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<AvatarViewSizesPage, AvatarViewSizesViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<DrawingViewPage, DrawingViewViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<ExpanderPage, ExpanderViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<MediaElementPage, MediaElementViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<MultiplePopupPage, MultiplePopupViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<PopupAnchorPage, PopupAnchorViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<PopupPositionPage, PopupPositionViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<ShowPopupInOnAppearingPage, ShowPopupInOnAppearingPageViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<SemanticOrderViewPage, SemanticOrderViewPageViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
	});

	public AppShell() => InitializeComponent();

	public static string GetPageRoute<TViewModel>() where TViewModel : BaseViewModel
	{
		return GetPageRoute(typeof(TViewModel));
	}

	public static string GetPageRoute(Type viewModelType)
	{
		if (!viewModelType.IsAssignableTo(typeof(BaseViewModel)))
		{
			throw new ArgumentException($"{nameof(viewModelType)} must implement {nameof(BaseViewModel)}", nameof(viewModelType));
		}

		if (!viewModelMappings.TryGetValue(viewModelType, out (Type GalleryPageType, Type ContentPageType) mapping))
		{
			throw new KeyNotFoundException($"No map for ${viewModelType} was found on navigation mappings. Please register your ViewModel in {nameof(AppShell)}.{nameof(viewModelMappings)}");
		}

		var uri = new UriBuilder("", GetPageRoute(mapping.GalleryPageType, mapping.ContentPageType));
		return uri.Uri.OriginalString[..^1];
	}

	static string GetPageRoute(Type galleryPageType, Type contentPageType) => $"//{galleryPageType.Name}/{contentPageType.Name}";

	static KeyValuePair<Type, (Type GalleryPageType, Type ContentPageType)> CreateViewModelMapping<TPage, TViewModel, TGalleryPage, TGalleryViewModel>() where TPage : BasePage<TViewModel>
																																							where TViewModel : BaseViewModel
																																							where TGalleryPage : BaseGalleryPage<TGalleryViewModel>
																																							where TGalleryViewModel : BaseGalleryViewModel
	{
		return new KeyValuePair<Type, (Type GalleryPageType, Type ContentPageType)>(typeof(TViewModel), (typeof(TGalleryPage), typeof(TPage)));
	}
}