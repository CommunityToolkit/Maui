using CommunityToolkit.Maui.Sample.Pages;
using CommunityToolkit.Maui.Sample.Pages.Alerts;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;
using CommunityToolkit.Maui.Sample.Pages.Converters;
using CommunityToolkit.Maui.Sample.Pages.Extensions;
using CommunityToolkit.Maui.Sample.Pages.Layouts;
using CommunityToolkit.Maui.Sample.Pages.Views;
using CommunityToolkit.Maui.Sample.ViewModels;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.Layouts;
using CommunityToolkit.Maui.Sample.ViewModels.Views;

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
		CreateViewModelMapping<MaskedBehaviorPage, MaskedBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<MaxLengthReachedBehaviorPage, MaxLengthReachedBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<MultiValidationBehaviorPage, MultiValidationBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<NumericValidationBehaviorPage, NumericValidationBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<ProgressBarAnimationBehaviorPage, ProgressBarAnimationBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<RequiredStringValidationBehaviorPage, RequiredStringValidationBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
		CreateViewModelMapping<SetFocusOnEntryCompletedBehaviorPage, SetFocusOnEntryCompletedBehaviorViewModel, BehaviorsGalleryPage, BehaviorsGalleryViewModel>(),
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

		// Add Extensions View Models
		CreateViewModelMapping<ColorAnimationExtensionsPage, ColorAnimationExtensionsViewModel, ExtensionsGalleryPage, ExtensionsGalleryViewModel>(),

		// Add Layouts View Models
		CreateViewModelMapping<UniformItemsLayoutPage, UniformItemsLayoutViewModel, LayoutsGalleryPage, LayoutsGalleryViewModel>(),

		// Add Views View Models
		CreateViewModelMapping<DrawingViewPage, DrawingViewViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<MultiplePopupPage, MultiplePopupViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<PopupAnchorPage, PopupAnchorViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
		CreateViewModelMapping<PopupPositionPage, PopupPositionViewModel, ViewsGalleryPage, ViewsGalleryViewModel>(),
	});

	public AppShell()
	{
		InitializeComponent();

		RegisterRouting();
	}

	public static string GetPageRoute(Type viewModelType)
	{
		if (!viewModelMappings.ContainsKey(viewModelType))
		{
			throw new KeyNotFoundException($"No map for ${viewModelType} was found on navigation mappings. Please register your ViewModel in {nameof(AppShell)}.{nameof(viewModelMappings)}");
		}

		(Type galleryPageType, Type contentPageType) = viewModelMappings[viewModelType];

		var uri = new UriBuilder("", GetPageRoute(galleryPageType, contentPageType));
		return uri.Uri.OriginalString[..^1];
	}

	static string GetPageRoute(Type galleryPageType, Type contentPageType) => $"//{galleryPageType.Name}/{contentPageType.Name}";

	static void RegisterRouting()
	{
		foreach (var viewModelKeyValuePair in viewModelMappings)
		{
			Routing.RegisterRoute(GetPageRoute(viewModelKeyValuePair.Value.GalleryPageType, viewModelKeyValuePair.Value.ContentPageType), viewModelKeyValuePair.Value.ContentPageType);
		}
	}

	static KeyValuePair<Type, (Type GalleryPageType, Type ContentPageType)> CreateViewModelMapping<TPage, TViewModel, TGalleryPage, TGalleryViewModel>() where TPage : BasePage<TViewModel>
																																							where TViewModel : BaseViewModel
																																							where TGalleryPage : BaseGalleryPage<TGalleryViewModel>
																																							where TGalleryViewModel : BaseGalleryViewModel
	{
		return new KeyValuePair<Type, (Type GalleryPageType, Type ContentPageType)>(typeof(TViewModel), (typeof(TGalleryPage), typeof(TPage)));
	}
}