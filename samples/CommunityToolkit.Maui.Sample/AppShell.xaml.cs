using CommunityToolkit.Maui.Sample.Pages.Alerts;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;
using CommunityToolkit.Maui.Sample.Pages.Converters;
using CommunityToolkit.Maui.Sample.Pages.Extensions;
using CommunityToolkit.Maui.Sample.Pages.Layouts;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.Layouts;

namespace CommunityToolkit.Maui.Sample;

public partial class AppShell : Shell
{
	static readonly IReadOnlyDictionary<Type, (Type GalleryPageType, Type ContentPageType)> viewModelMappings = new Dictionary<Type, (Type, Type)>()
	{
		// Add Alerts View Models
		{ typeof(SnackbarViewModel), (typeof(AlertsGalleryPage), typeof(SnackbarPage)) },
		{ typeof(ToastViewModel), (typeof(AlertsGalleryPage), typeof(ToastPage)) },

		// Add Behaviors View Models
		{ typeof(CharactersValidationBehaviorViewModel), (typeof(BehaviorsGalleryPage), typeof(CharactersValidationBehaviorPage)) },
		{ typeof(EmailValidationBehaviorViewModel), (typeof(BehaviorsGalleryPage), typeof(EmailValidationBehaviorPage)) },
		{ typeof(EventToCommandBehaviorViewModel), (typeof(BehaviorsGalleryPage), typeof(EventToCommandBehaviorPage)) },
		{ typeof(MaskedBehaviorViewModel), (typeof(BehaviorsGalleryPage), typeof(MaskedBehaviorPage)) },
		{ typeof(MaxLengthReachedBehaviorViewModel), (typeof(BehaviorsGalleryPage), typeof(MaxLengthReachedBehaviorPage)) },
		{ typeof(MultiValidationBehaviorViewModel), (typeof(BehaviorsGalleryPage), typeof(MultiValidationBehaviorPage)) },
		{ typeof(NumericValidationBehaviorViewModel), (typeof(BehaviorsGalleryPage), typeof(NumericValidationBehaviorPage)) },
		{ typeof(ProgressBarAnimationBehaviorViewModel), (typeof(BehaviorsGalleryPage), typeof(ProgressBarAnimationBehaviorPage)) },
		{ typeof(RequiredStringValidationBehaviorViewModel), (typeof(BehaviorsGalleryPage), typeof(RequiredStringValidationBehaviorPage)) },
		{ typeof(SetFocusOnEntryCompletedBehaviorViewModel), (typeof(BehaviorsGalleryPage), typeof(SetFocusOnEntryCompletedBehaviorPage)) },
		{ typeof(TextValidationBehaviorViewModel), (typeof(BehaviorsGalleryPage), typeof(TextValidationBehaviorPage)) },
		{ typeof(UriValidationBehaviorViewModel), (typeof(BehaviorsGalleryPage), typeof(UriValidationBehaviorPage)) },
		{ typeof(UserStoppedTypingBehaviorViewModel), (typeof(BehaviorsGalleryPage), typeof(UserStoppedTypingBehaviorPage)) },

		// Add Converters View Models
		{ typeof(BoolToObjectConverterViewModel), (typeof(ConvertersGalleryPage), typeof(BoolToObjectConverterPage)) },
		{ typeof(ColorsConvertersViewModel), (typeof(ConvertersGalleryPage), typeof(ColorsConverterPage)) },
		{ typeof(CompareConverterViewModel), (typeof(ConvertersGalleryPage), typeof(CompareConverterPage)) },
		{ typeof(DateTimeOffsetConverterViewModel), (typeof(ConvertersGalleryPage), typeof(DateTimeOffsetConverterPage)) },
		{ typeof(DoubleToIntConverterViewModel), (typeof(ConvertersGalleryPage), typeof(DoubleToIntConverterPage)) },
		{ typeof(EnumToBoolConverterViewModel), (typeof(ConvertersGalleryPage), typeof(EnumToBoolConverterPage)) },
		{ typeof(EnumToIntConverterViewModel), (typeof(ConvertersGalleryPage), typeof(EnumToIntConverterPage)) },
		{ typeof(EqualConverterViewModel), (typeof(ConvertersGalleryPage), typeof(EqualConverterPage)) },
		{ typeof(NotEqualConverterViewModel), (typeof(ConvertersGalleryPage), typeof(NotEqualConverterPage)) },
		{ typeof(ImageResourceConverterViewModel), (typeof(ConvertersGalleryPage), typeof(ImageResourceConverterPage)) },
		{ typeof(IndexToArrayItemConverterViewModel), (typeof(ConvertersGalleryPage), typeof(IndexToArrayItemConverterPage)) },
		{ typeof(IntToBoolConverterViewModel), (typeof(ConvertersGalleryPage), typeof(IntToBoolConverterPage)) },
		{ typeof(InvertedBoolConverterViewModel), (typeof(ConvertersGalleryPage), typeof(InvertedBoolConverterPage)) },
		{ typeof(IsListNotNullOrEmptyConverterViewModel), (typeof(ConvertersGalleryPage), typeof(IsListNotNullOrEmptyConverterPage)) },
		{ typeof(IsListNullOrEmptyConverterViewModel), (typeof(ConvertersGalleryPage), typeof(IsListNullOrEmptyConverterPage)) },
		{ typeof(IsStringNotNullOrEmptyConverterViewModel), (typeof(ConvertersGalleryPage), typeof(IsStringNotNullOrEmptyConverterPage)) },
		{ typeof(IsStringNotNullOrWhiteSpaceConverterViewModel), (typeof(ConvertersGalleryPage), typeof(IsStringNotNullOrWhiteSpaceConverterPage)) },
		{ typeof(IsStringNullOrEmptyConverterViewModel), (typeof(ConvertersGalleryPage), typeof(IsStringNullOrEmptyConverterPage)) },
		{ typeof(IsStringNullOrWhiteSpaceConverterViewModel), (typeof(ConvertersGalleryPage), typeof(IsStringNullOrWhiteSpaceConverterPage)) },
		{ typeof(ItemSelectedEventArgsConverterViewModel), (typeof(ConvertersGalleryPage), typeof(ItemSelectedEventArgsConverterPage)) },
		{ typeof(ItemTappedEventArgsConverterViewModel), (typeof(ConvertersGalleryPage), typeof(ItemTappedEventArgsConverterPage)) },
		{ typeof(ListToStringConverterViewModel), (typeof(ConvertersGalleryPage), typeof(ListToStringConverterPage)) },
		{ typeof(MathExpressionConverterViewModel), (typeof(ConvertersGalleryPage), typeof(MathExpressionConverterPage)) },
		{ typeof(MultiConverterViewModel), (typeof(ConvertersGalleryPage), typeof(MultiConverterPage)) },
		{ typeof(StringToListConverterViewModel), (typeof(ConvertersGalleryPage), typeof(StringToListConverterPage)) },
		{ typeof(TextCaseConverterViewModel), (typeof(ConvertersGalleryPage), typeof(TextCaseConverterPage)) },
		{ typeof(VariableMultiValueConverterViewModel), (typeof(ConvertersGalleryPage), typeof(VariableMultiValueConverterPage)) },

		// Add Extensions View Models
		{ typeof(ColorAnimationExtensionsViewModel), (typeof(ExtensionsGalleryPage), typeof(ColorAnimationExtensionsPage)) },

		// Add Layouts View Models
		{ typeof(UniformItemsLayoutViewModel), (typeof(LayoutsGalleryPage), typeof(UniformItemsLayoutPage)) },
	};

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

		(Type GalleryPageType, Type ContentPageType) = viewModelMappings[viewModelType];

		var uri = new UriBuilder("", GetPageRoute(GalleryPageType, ContentPageType));
		return uri.Uri.OriginalString[..^1];
	}

	static string GetPageRoute(Type GalleryPageType, Type ContentPageType) => $"//{GalleryPageType.Name}/{ContentPageType.Name}";

	static void RegisterRouting()
	{
		foreach(var viewModelKeyValuePair in viewModelMappings)
		{
			Routing.RegisterRoute(GetPageRoute(viewModelKeyValuePair.Value.GalleryPageType, viewModelKeyValuePair.Value.ContentPageType), viewModelKeyValuePair.Value.ContentPageType);
		}
	}
}
