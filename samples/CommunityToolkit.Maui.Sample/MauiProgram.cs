using CommunityToolkit.Maui.Sample.ViewModels;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Sample.ViewModels.Layouts;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace CommunityToolkit.Maui.Sample;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>().UseMauiCommunityToolkit();

		// Add HttpClient
		builder.Services.AddHttpClient<ByteArrayToImageSourceConverterViewModel>();

		// Add Gallery View Models
		builder.Services.AddTransient<AlertsGalleryViewModel>();
		builder.Services.AddTransient<BehaviorsGalleryViewModel>();
		builder.Services.AddTransient<ConvertersGalleryViewModel>();
		builder.Services.AddTransient<ExtensionsGalleryViewModel>();
		builder.Services.AddTransient<LayoutsGalleryViewModel>();
		builder.Services.AddTransient<ViewsGalleryViewModel>();

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
		builder.Services.AddTransient<ImageResourceConverterViewModel>();
		builder.Services.AddTransient<IndexToArrayItemConverterViewModel>();
		builder.Services.AddTransient<IntToBoolConverterViewModel>();
		builder.Services.AddTransient<InvertedBoolConverterViewModel>();
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

		// Add Layouts View Models
		builder.Services.AddTransient<UniformItemsLayoutViewModel>();

		// Add Views View Models
		builder.Services.AddTransient<CsharpBindingPopupViewModel>();
		builder.Services.AddTransient<MultiplePopupViewModel>();
		builder.Services.AddTransient<PopupAnchorViewModel>();
		builder.Services.AddTransient<PopupPositionViewModel>();
		builder.Services.AddTransient<XamlBindingPopupViewModel>();

		return builder.Build();
	}
}
