using CommunityToolkit.Maui.Sample.Pages.Alerts;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;
using CommunityToolkit.Maui.Sample.Pages.Converters;
using CommunityToolkit.Maui.Sample.Pages.Extensions;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample;

public partial class AppShell : Shell
{
	static readonly IReadOnlyDictionary<Type, string> viewModelMappings = new Dictionary<Type, string>()
	{
		// Add Alerts View Models
		{ typeof(SnackbarViewModel), $"//{nameof(AlertsGalleryPage)}/{nameof(SnackbarPage)}" },

		// Add Behaviors View Models
		{ typeof(CharactersValidationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(CharactersValidationBehaviorPage)}" },
		{ typeof(EmailValidationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(EmailValidationBehaviorPage)}" },
		{ typeof(EventToCommandBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(EventToCommandBehaviorPage)}" },
		{ typeof(MaskedBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(MaskedBehaviorPage)}" },
		{ typeof(MaxLengthReachedBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(MaxLengthReachedBehaviorPage)}" },
		{ typeof(MultiValidationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(MultiValidationBehaviorPage)}" },
		{ typeof(NumericValidationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(NumericValidationBehaviorPage)}" },
		{ typeof(ProgressBarAnimationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(ProgressBarAnimationBehaviorPage)}" },
		{ typeof(RequiredStringValidationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(RequiredStringValidationBehaviorPage)}" },
		{ typeof(SetFocusOnEntryCompletedBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(SetFocusOnEntryCompletedBehaviorPage)}" },
		{ typeof(TextValidationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(TextValidationBehaviorPage)}" },
		{ typeof(UriValidationBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(UriValidationBehaviorPage)}" },
		{ typeof(UserStoppedTypingBehaviorViewModel), $"//{nameof(BehaviorsGalleryPage)}/{nameof(UserStoppedTypingBehaviorPage)}" },

		// Add Converters View Models
		{ typeof(BoolToObjectConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(BoolToObjectConverterPage)}" },
		{ typeof(ColorsConvertersViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(ColorsConverterPage)}" },
		{ typeof(DateTimeOffsetConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(DateTimeOffsetConverterPage)}" },
		{ typeof(DoubleToIntConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(DoubleToIntConverterPage)}" },
		{ typeof(EnumToBoolConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(EnumToBoolConverterPage)}" },
		{ typeof(EnumToIntConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(EnumToIntConverterPage)}" },
		{ typeof(EqualConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(EqualConverterPage)}" },
		{ typeof(NotEqualConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(NotEqualConverterPage)}" },
		{ typeof(ImageResourceConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(ImageResourceConverterPage)}" },
		{ typeof(IndexToArrayItemConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(IndexToArrayItemConverterPage)}" },
		{ typeof(IntToBoolConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(IntToBoolConverterPage)}" },
		{ typeof(InvertedBoolConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(InvertedBoolConverterPage)}" },
		{ typeof(IsNotNullOrEmptyConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(IsNotNullOrEmptyConverterPage)}" },
		{ typeof(IsNullOrEmptyConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(IsNullOrEmptyConverterPage)}" },
		{ typeof(ItemTappedEventArgsViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(ItemTappedEventArgsPage)}" },
		{ typeof(ListIsNotNullOrEmptyConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(ListIsNotNullOrEmptyConverterPage)}" },
		{ typeof(ListIsNullOrEmptyConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(ListIsNullOrEmptyConverterPage)}" },
		{ typeof(ListToStringConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(ListToStringConverterPage)}" },
		{ typeof(MathExpressionConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(MathExpressionConverterPage)}" },
		{ typeof(MultiConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(MultiConverterPage)}" },
		{ typeof(StringToListConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(StringToListConverterPage)}" },
		{ typeof(TextCaseConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(TextCaseConverterPage)}" },
		{ typeof(VariableMultiValueConverterViewModel), $"//{nameof(ConvertersGalleryPage)}/{nameof(VariableMultiValueConverterPage)}" },

		// Add Extensions View Models
		{ typeof(ColorAnimationExtensionsViewModel), $"//{nameof(ExtensionsGalleryPage)}/{nameof(ColorAnimationExtensionsPage)}" },
	};

	public AppShell()
	{
		InitializeComponent();

		RegisterRouting();
	}

	public static string GetPageRoute(Type viewModelType)
	{
		var uri = new UriBuilder("", GetPagePathForViewModel(viewModelType));
		return uri.Uri.OriginalString[..^1];
	}

	static string GetPagePathForViewModel(Type viewModelType)
	{
		if (!viewModelMappings.ContainsKey(viewModelType))
		{
			throw new KeyNotFoundException($"No map for ${viewModelType} was found on navigation mappings");
		}

		return viewModelMappings[viewModelType];
	}

	static void RegisterRouting()
	{
		// Register Alerts Pages
		Routing.RegisterRoute($"//{nameof(AlertsGalleryPage)}/{nameof(SnackbarPage)}", typeof(SnackbarPage));

		// Register Behaviors Pages
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

		// Register Converters Pages
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

		// Register Extensions Pages
		Routing.RegisterRoute($"//{nameof(ExtensionsGalleryPage)}/{nameof(ColorAnimationExtensionsPage)}", typeof(ColorAnimationExtensionsPage));
	}
}
