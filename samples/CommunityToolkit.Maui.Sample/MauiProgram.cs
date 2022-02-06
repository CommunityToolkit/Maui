using CommunityToolkit.Maui.Sample.Pages;
using CommunityToolkit.Maui.Sample.Pages.Alerts;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;
using CommunityToolkit.Maui.Sample.Pages.Converters;
using CommunityToolkit.Maui.Sample.Pages.Extensions;
using CommunityToolkit.Maui.Sample.ViewModels;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;

[assembly: XamlCompilationAttribute(XamlCompilationOptions.Compile)]

namespace CommunityToolkit.Maui.Sample;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>().UseMauiCommunityToolkit();

		// Add Gallery Pages
		builder.Services.AddTransient<AlertsGalleryPage>();
		builder.Services.AddTransient<BehaviorsGalleryPage>();
		builder.Services.AddTransient<ConvertersGalleryPage>();
		builder.Services.AddTransient<ExtensionsGalleryPage>();
		builder.Services.AddTransient<MainGalleryPage>();

		// Add Gallery View Models
		builder.Services.AddTransient<AlertsGalleryViewModel>();
		builder.Services.AddTransient<BehaviorsGalleryViewModel>();
		builder.Services.AddTransient<ConvertersGalleryViewModel>();
		builder.Services.AddTransient<ExtensionsGalleryViewModel>();
		builder.Services.AddTransient<MainGalleryViewModel>();

		// Add Alerts Pages
		builder.Services.AddTransient<SnackbarPage>();

		// Add Alerts View Models

		// Add Behaviors Pages
		builder.Services.AddTransient<CharactersValidationBehaviorPage>();
		builder.Services.AddTransient<EmailValidationBehaviorPage>();
		builder.Services.AddTransient<EventToCommandBehaviorPage>();
		builder.Services.AddTransient<MaskedBehaviorPage>();
		builder.Services.AddTransient<MaxLengthReachedBehaviorPage>();
		builder.Services.AddTransient<MultiValidationBehaviorPage>();
		builder.Services.AddTransient<NumericValidationBehaviorPage>();
		builder.Services.AddTransient<ProgressBarAnimationBehaviorPage>();
		builder.Services.AddTransient<RequiredStringValidationBehaviorPage>();
		builder.Services.AddTransient<SetFocusOnEntryCompletedBehaviorPage>();
		builder.Services.AddTransient<TextValidationBehaviorPage>();
		builder.Services.AddTransient<UriValidationBehaviorPage>();
		builder.Services.AddTransient<UserStoppedTypingBehaviorPage>();

		// Add Behaviors View Models
		builder.Services.AddTransient<EventToCommandBehaviorViewModel>();
		builder.Services.AddTransient<MaxLengthReachedBehaviorViewModel>();
		builder.Services.AddTransient<ProgressBarAnimationBehaviorViewModel>();
		builder.Services.AddTransient<UserStoppedTypingBehaviorViewModel>();

		// Add Converters Pages
		builder.Services.AddTransient<BoolToObjectConverterPage>();
		builder.Services.AddTransient<ColorsConverterPage>();
		builder.Services.AddTransient<DateTimeOffsetConverterPage>();
		builder.Services.AddTransient<DoubleToIntConverterPage>();
		builder.Services.AddTransient<EnumToBoolConverterPage>();
		builder.Services.AddTransient<EnumToIntConverterPage>();
		builder.Services.AddTransient<EqualConverterPage>();
		builder.Services.AddTransient<ImageResourceConverterPage>();
		builder.Services.AddTransient<IndexToArrayItemConverterPage>();
		builder.Services.AddTransient<IntToBoolConverterPage>();
		builder.Services.AddTransient<InvertedBoolConverterPage>();
		builder.Services.AddTransient<IsNotNullOrEmptyConverterPage>();
		builder.Services.AddTransient<IsNullOrEmptyConverterPage>();
		builder.Services.AddTransient<ItemTappedEventArgsPage>();
		builder.Services.AddTransient<ListIsNotNullOrEmptyConverterPage>();
		builder.Services.AddTransient<ListIsNullOrEmptyConverterPage>();
		builder.Services.AddTransient<ListToStringConverterPage>();
		builder.Services.AddTransient<MathExpressionConverterPage>();
		builder.Services.AddTransient<MultiConverterPage>();
		builder.Services.AddTransient<NotEqualConverterPage>();
		builder.Services.AddTransient<StringToListConverterPage>();
		builder.Services.AddTransient<TextCaseConverterPage>();
		builder.Services.AddTransient<VariableMultiValueConverterPage>();
		builder.Services.AddTransient<ItemSelectedEventArgsConverterPage>();

		// Add Converters View Models
		builder.Services.AddTransient<DateTimeOffsetConverterViewModel>();
		builder.Services.AddTransient<DoubleToIntConverterViewModel>();
		builder.Services.AddTransient<EnumToIntConverterViewModel>();
		builder.Services.AddTransient<EqualConverterViewModel>();
		builder.Services.AddTransient<IndexToArrayItemConverterViewModel>();
		builder.Services.AddTransient<IntToBoolConverterViewModel>();
		builder.Services.AddTransient<IsNotNullOrEmptyConverterViewModel>();
		builder.Services.AddTransient<IsNullOrEmptyConverterViewModel>();
		builder.Services.AddTransient<ItemTappedEventArgsViewModel>();
		builder.Services.AddTransient<ListIsNotNullOrEmptyConverterViewModel>();
		builder.Services.AddTransient<ListIsNullOrEmptyConverterViewModel>();
		builder.Services.AddTransient<ListToStringConverterViewModel>();
		builder.Services.AddTransient<ItemSelectedEventArgsConverterViewModel>();

		// Add Extensions Pages
		builder.Services.AddTransient<ColorAnimationExtensionsPage>();

		// Add Extensions View Models

		return builder.Build();
	}
}