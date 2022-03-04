using CommunityToolkit.Maui.Sample.ViewModels;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.Layouts;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace CommunityToolkit.Maui.Sample;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>().UseMauiCommunityToolkit();

		// Add Gallery View Models
		builder.Services.AddTransient<AlertsGalleryViewModel>();
		builder.Services.AddTransient<BehaviorsGalleryViewModel>();
		builder.Services.AddTransient<ConvertersGalleryViewModel>();
		builder.Services.AddTransient<ExtensionsGalleryViewModel>();
		builder.Services.AddTransient<LayoutsGalleryViewModel>();
		builder.Services.AddTransient<MainGalleryViewModel>();

		// Add Alerts View Models
		builder.Services.AddTransient<SnackbarViewModel>();
		builder.Services.AddTransient<ToastViewModel>();

		// Add Behaviors View Models
		builder.Services.AddTransient<CharactersValidationBehaviorViewModel>();
		builder.Services.AddTransient<EmailValidationBehaviorViewModel>();
		builder.Services.AddTransient<EventToCommandBehaviorViewModel>();
		builder.Services.AddTransient<MaskedBehaviorViewModel>();
		builder.Services.AddTransient<MaxLengthReachedBehaviorViewModel>();
		builder.Services.AddTransient<MultiValidationBehaviorViewModel>();
		builder.Services.AddTransient<NumericValidationBehaviorViewModel>();
		builder.Services.AddTransient<ProgressBarAnimationBehaviorViewModel>();
		builder.Services.AddTransient<RequiredStringValidationBehaviorViewModel>();
		builder.Services.AddTransient<SetFocusOnEntryCompletedBehaviorViewModel>();
		builder.Services.AddTransient<TextValidationBehaviorViewModel>();
		builder.Services.AddTransient<UriValidationBehaviorViewModel>();
		builder.Services.AddTransient<UserStoppedTypingBehaviorViewModel>();

		// Add Converters View Models
		builder.Services.AddTransient<BoolToObjectConverterViewModel>();
		builder.Services.AddTransient<ByteArrayToImageSourceConverterViewModel>();
		builder.Services.AddTransient<ColorsConverterViewModel>();
		builder.Services.AddTransient<CompareConverterViewModel>();
		builder.Services.AddTransient<DateTimeOffsetConverterViewModel>();
		builder.Services.AddTransient<DoubleToIntConverterViewModel>();
		builder.Services.AddTransient<EnumToBoolConverterViewModel>();
		builder.Services.AddTransient<EnumToIntConverterViewModel>();
		builder.Services.AddTransient<EqualConverterViewModel>();
		builder.Services.AddTransient<IndexToArrayItemConverterViewModel>();
		builder.Services.AddTransient<IntToBoolConverterViewModel>();
		builder.Services.AddTransient<IsListNotNullOrEmptyConverterViewModel>();
		builder.Services.AddTransient<IsListNullOrEmptyConverterViewModel>();
		builder.Services.AddTransient<IsStringNotNullOrEmptyConverterViewModel>();
		builder.Services.AddTransient<IsStringNotNullOrWhiteSpaceConverterViewModel>();
		builder.Services.AddTransient<IsStringNullOrEmptyConverterViewModel>();
		builder.Services.AddTransient<IsStringNullOrWhiteSpaceConverterViewModel>();
		builder.Services.AddTransient<ItemSelectedEventArgsConverterViewModel>();
		builder.Services.AddTransient<ItemTappedEventArgsConverterViewModel>();
		builder.Services.AddTransient<ListToStringConverterViewModel>();
		builder.Services.AddTransient<MathExpressionConverterViewModel>();
		builder.Services.AddTransient<MultiConverterViewModel>();
		builder.Services.AddTransient<NotEqualConverterViewModel>();
		builder.Services.AddTransient<StringToListConverterViewModel>();
		builder.Services.AddTransient<TextCaseConverterViewModel>();
		builder.Services.AddTransient<VariableMultiValueConverterViewModel>();

		// Add Extensions
		builder.Services.AddTransient<ColorAnimationExtensionsViewModel>();

		// Add Layouts
		builder.Services.AddTransient<UniformItemsLayoutViewModel>();

		return builder.Build();
	}
}