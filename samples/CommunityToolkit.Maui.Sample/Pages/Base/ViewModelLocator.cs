using CommunityToolkit.Maui.Sample.Pages.Alerts;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;
using CommunityToolkit.Maui.Sample.Pages.Converters;
using CommunityToolkit.Maui.Sample.Pages.Extensions;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages;

static class ViewModelLocator
{
	internal static readonly Dictionary<Type, string> mappings;

	static ViewModelLocator()
	{
		mappings = new Dictionary<Type, string>();
		CreatePageViewModelMappings();
		RegisterRouting();
	}

	static void CreatePageViewModelMappings()
	{
		// Add Converters ViewModels
		mappings.Add(typeof(BoolToObjectConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(BoolToObjectConverterPage)}");
		mappings.Add(typeof(ColorsConvertersViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(ColorsConverterPage)}");
		mappings.Add(typeof(DateTimeOffsetConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(DateTimeOffsetConverterPage)}");
		mappings.Add(typeof(DoubleToIntConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(DoubleToIntConverterPage)}");
		mappings.Add(typeof(EnumToBoolConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(EnumToBoolConverterPage)}");
		mappings.Add(typeof(EnumToIntConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(EnumToIntConverterPage)}");
		mappings.Add(typeof(EqualConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(EqualConverterPage)}");
		mappings.Add(typeof(NotEqualConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(NotEqualConverterPage)}");
		mappings.Add(typeof(ImageResourceConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(ImageResourceConverterPage)}");
		mappings.Add(typeof(IndexToArrayItemConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(IndexToArrayItemConverterPage)}");
		mappings.Add(typeof(IntToBoolConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(IntToBoolConverterPage)}");
		mappings.Add(typeof(InvertedBoolConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(InvertedBoolConverterPage)}");
		mappings.Add(typeof(IsNotNullOrEmptyConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(IsNotNullOrEmptyConverterPage)}");
		mappings.Add(typeof(IsNullOrEmptyConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(IsNullOrEmptyConverterPage)}");
		mappings.Add(typeof(ItemTappedEventArgsViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(ItemTappedEventArgsPage)}");
		mappings.Add(typeof(ListIsNotNullOrEmptyConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(ListIsNotNullOrEmptyConverterPage)}");
		mappings.Add(typeof(ListIsNullOrEmptyConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(ListIsNullOrEmptyConverterPage)}");
		mappings.Add(typeof(ListToStringConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(ListToStringConverterPage)}");
		mappings.Add(typeof(MathExpressionConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(MathExpressionConverterPage)}");
		mappings.Add(typeof(MultiConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(MultiConverterPage)}");
		mappings.Add(typeof(StringToListConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(StringToListConverterPage)}");
		mappings.Add(typeof(TextCaseConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(TextCaseConverterPage)}");
		mappings.Add(typeof(VariableMultiValueConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(VariableMultiValueConverterPage)}");

		// Add Alerts View Models
		mappings.Add(typeof(SnackbarViewModel), $"//{nameof(AlertsGalleryPage)}/{nameof(SnackbarPage)}");

		// Add Behaviors View Models
		mappings.Add(typeof(CharactersValidationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(CharactersValidationBehaviorPage)}");
		mappings.Add(typeof(EmailValidationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(EmailValidationBehaviorPage)}");
		mappings.Add(typeof(EventToCommandBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(EventToCommandBehaviorPage)}");
		mappings.Add(typeof(MaskedBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(MaskedBehaviorPage)}");
		mappings.Add(typeof(MaxLengthReachedBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(MaxLengthReachedBehaviorPage)}");
		mappings.Add(typeof(MultiValidationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(MultiValidationBehaviorPage)}");
		mappings.Add(typeof(NumericValidationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(NumericValidationBehaviorPage)}");
		mappings.Add(typeof(ProgressBarAnimationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(ProgressBarAnimationBehaviorPage)}");
		mappings.Add(typeof(RequiredStringValidationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(RequiredStringValidationBehaviorPage)}");
		mappings.Add(typeof(SetFocusOnEntryCompletedBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(SetFocusOnEntryCompletedBehaviorPage)}");
		mappings.Add(typeof(TextValidationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(TextValidationBehaviorPage)}");
		mappings.Add(typeof(UriValidationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(UriValidationBehaviorPage)}");
		mappings.Add(typeof(UserStoppedTypingBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(UserStoppedTypingBehaviorPage)}");

		// Add Extensions View Models
		mappings.Add(typeof(ColorAnimationExtensionsViewModel), $"//{nameof(ExtensionsGalleryPage)}/{nameof(ColorAnimationExtensionsPage)}");
	}

	static void RegisterRouting()
	{
		// Add Converters Pages
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(BoolToObjectConverterPage)}", typeof(BoolToObjectConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(ColorsConverterPage)}", typeof(ColorsConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(DateTimeOffsetConverterPage)}", typeof(DateTimeOffsetConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(DoubleToIntConverterPage)}", typeof(DoubleToIntConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(EnumToBoolConverterPage)}", typeof(EnumToBoolConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(EnumToIntConverterPage)}", typeof(EnumToIntConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(EqualConverterPage)}", typeof(EqualConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(NotEqualConverterPage)}", typeof(NotEqualConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(ImageResourceConverterPage)}", typeof(ImageResourceConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(IndexToArrayItemConverterPage)}", typeof(IndexToArrayItemConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(IntToBoolConverterPage)}", typeof(IntToBoolConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(InvertedBoolConverterPage)}", typeof(InvertedBoolConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(IsNotNullOrEmptyConverterPage)}", typeof(IsNotNullOrEmptyConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(IsNullOrEmptyConverterPage)}", typeof(IsNullOrEmptyConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(ItemTappedEventArgsPage)}", typeof(ItemTappedEventArgsPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(ListIsNotNullOrEmptyConverterPage)}", typeof(ListIsNotNullOrEmptyConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(ListIsNullOrEmptyConverterPage)}", typeof(ListIsNotNullOrEmptyConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(ListToStringConverterPage)}", typeof(ListToStringConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(MathExpressionConverterPage)}", typeof(MathExpressionConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(MultiConverterPage)}", typeof(MultiConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(StringToListConverterPage)}", typeof(StringToListConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(TextCaseConverterPage)}", typeof(TextCaseConverterPage));
		Routing.RegisterRoute($"//{nameof(ConvertersGalleryPage)}/{nameof(VariableMultiValueConverterPage)}", typeof(VariableMultiValueConverterPage));

		// Add Alerts Pages
		Routing.RegisterRoute($"//{nameof(AlertsGalleryPage)}/{nameof(SnackbarPage)}", typeof(SnackbarPage));

		// Add Behaviors Pages
		Routing.RegisterRoute($"//{nameof(BehaviorsGalleryPage)}/{nameof(CharactersValidationBehaviorPage)}", typeof(CharactersValidationBehaviorPage));
		Routing.RegisterRoute($"//{nameof(BehaviorsGalleryPage)}/{nameof(EmailValidationBehaviorPage)}", typeof(EmailValidationBehaviorPage));
		Routing.RegisterRoute($"//{nameof(BehaviorsGalleryPage)}/{nameof(EventToCommandBehaviorPage)}", typeof(EventToCommandBehaviorPage));
		Routing.RegisterRoute($"//{nameof(BehaviorsGalleryPage)}/{nameof(MaskedBehaviorPage)}", typeof(MaskedBehaviorPage));
		Routing.RegisterRoute($"//{nameof(BehaviorsGalleryPage)}/{nameof(MaxLengthReachedBehaviorPage)}", typeof(MaxLengthReachedBehaviorPage));
		Routing.RegisterRoute($"//{nameof(BehaviorsGalleryPage)}/{nameof(MultiValidationBehaviorPage)}", typeof(MultiValidationBehaviorPage));
		Routing.RegisterRoute($"//{nameof(BehaviorsGalleryPage)}/{nameof(NumericValidationBehaviorPage)}", typeof(NumericValidationBehaviorPage));
		Routing.RegisterRoute($"//{nameof(BehaviorsGalleryPage)}/{nameof(ProgressBarAnimationBehaviorPage)}", typeof(ProgressBarAnimationBehaviorPage));
		Routing.RegisterRoute($"//{nameof(BehaviorsGalleryPage)}/{nameof(RequiredStringValidationBehaviorPage)}", typeof(RequiredStringValidationBehaviorPage));
		Routing.RegisterRoute($"//{nameof(BehaviorsGalleryPage)}/{nameof(SetFocusOnEntryCompletedBehaviorPage)}", typeof(SetFocusOnEntryCompletedBehaviorPage));
		Routing.RegisterRoute($"//{nameof(BehaviorsGalleryPage)}/{nameof(TextValidationBehaviorPage)}", typeof(TextValidationBehaviorPage));
		Routing.RegisterRoute($"//{nameof(BehaviorsGalleryPage)}/{nameof(UriValidationBehaviorPage)}", typeof(UriValidationBehaviorPage));
		Routing.RegisterRoute($"//{nameof(BehaviorsGalleryPage)}/{nameof(UserStoppedTypingBehaviorPage)}", typeof(UserStoppedTypingBehaviorPage));

		// Add extension pages
		Routing.RegisterRoute($"//{nameof(ExtensionsGalleryPage)}/{nameof(ColorAnimationExtensionsPage)}", typeof(ColorAnimationExtensionsPage));
	}
}