using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Alerts;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;
using CommunityToolkit.Maui.Sample.Pages.Converters;
using CommunityToolkit.Maui.Sample.Pages.Extensions;
using CommunityToolkit.Maui.Sample.Pages.Layouts;
using CommunityToolkit.Maui.Sample.Pages.Views;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.Layouts;
using CommunityToolkit.Maui.Sample.ViewModels.Views;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace CommunityToolkit.Maui.Sample;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder()
								.UseMauiApp<App>()
								.UseMauiCommunityToolkit()
								.UseMauiCommunityToolkitMarkup();

		builder.Services.AddHttpClient<ByteArrayToImageSourceConverterViewModel>();
		builder.Services.AddSingleton<PopupSizeConstants>();

		RegisterPages(builder.Services);
		RegisterViewModels(builder.Services);
		RegisterEssentials(builder.Services);

		return builder.Build();
	}

	static void RegisterPages(in IServiceCollection services)
	{
		// Add Gallery Pages
		services.AddTransient<AlertsGalleryPage>();
		services.AddTransient<BehaviorsGalleryPage>();
		services.AddTransient<ConvertersGalleryPage>();
		services.AddTransient<ExtensionsGalleryPage>();
		services.AddTransient<LayoutsGalleryPage>();
		services.AddTransient<ViewsGalleryPage>();

		// Add Alerts Pages
		services.AddTransient<SnackbarPage>();
		services.AddTransient<ToastPage>();

		// Add Behaviors Pages
		services.AddTransient<CharactersValidationBehaviorPage>();
		services.AddTransient<EmailValidationBehaviorPage>();
		services.AddTransient<EventToCommandBehaviorPage>();
		services.AddTransient<MaskedBehaviorPage>();
		services.AddTransient<MaxLengthReachedBehaviorPage>();
		services.AddTransient<MultiValidationBehaviorPage>();
		services.AddTransient<NumericValidationBehaviorPage>();
		services.AddTransient<ProgressBarAnimationBehaviorPage>();
		services.AddTransient<RequiredStringValidationBehaviorPage>();
		services.AddTransient<SelectAllTextBehaviorPage>();
		services.AddTransient<SetFocusOnEntryCompletedBehaviorPage>();
		services.AddTransient<TextValidationBehaviorPage>();
		services.AddTransient<UriValidationBehaviorPage>();
		services.AddTransient<UserStoppedTypingBehaviorPage>();

		// Add Converters Pages
		services.AddTransient<BoolToObjectConverterPage>();
		services.AddTransient<StateToBooleanConverterPage>();
		services.AddTransient<ByteArrayToImageSourceConverterPage>();
		services.AddTransient<ColorsConverterPage>();
		services.AddTransient<CompareConverterPage>();
		services.AddTransient<DateTimeOffsetConverterPage>();
		services.AddTransient<DoubleToIntConverterPage>();
		services.AddTransient<EnumToBoolConverterPage>();
		services.AddTransient<EnumToIntConverterPage>();
		services.AddTransient<ImageResourceConverterPage>();
		services.AddTransient<IndexToArrayItemConverterPage>();
		services.AddTransient<IntToBoolConverterPage>();
		services.AddTransient<InvertedBoolConverterPage>();
		services.AddTransient<IsEqualConverterPage>();
		services.AddTransient<IsListNotNullOrEmptyConverterPage>();
		services.AddTransient<IsListNullOrEmptyConverterPage>();
		services.AddTransient<IsNotEqualConverterPage>();
		services.AddTransient<IsNotNullConverterPage>();
		services.AddTransient<IsNullConverterPage>();
		services.AddTransient<IsStringNotNullOrEmptyConverterPage>();
		services.AddTransient<IsStringNotNullOrWhiteSpaceConverterPage>();
		services.AddTransient<IsStringNullOrEmptyConverterPage>();
		services.AddTransient<IsStringNullOrWhiteSpaceConverterPage>();
		services.AddTransient<ItemTappedEventArgsConverterPage>();
		services.AddTransient<ListToStringConverterPage>();
		services.AddTransient<MathExpressionConverterPage>();
		services.AddTransient<MultiConverterPage>();
		services.AddTransient<MultiMathExpressionConverterPage>();
		services.AddTransient<SelectedItemEventArgsConverterPage>();
		services.AddTransient<StringToListConverterPage>();
		services.AddTransient<TextCaseConverterPage>();
		services.AddTransient<VariableMultiValueConverterPage>();

		// Add Extensions Pages
		services.AddTransient<ColorAnimationExtensionsPage>();

		// Add Layouts Pages
		services.AddTransient<UniformItemsLayoutPage>();

		// Add Views Pages
		services.AddTransient<CsharpBindingPopup>();
		services.AddTransient<DrawingViewPage>();
		services.AddTransient<MultiplePopupPage>();
		services.AddTransient<PopupAnchorPage>();
		services.AddTransient<PopupPositionPage>();
		services.AddTransient<XamlBindingPopup>();
	}

	static void RegisterViewModels(in IServiceCollection services)
	{
		// Add Gallery View Models
		services.AddTransient<AlertsGalleryViewModel>();
		services.AddTransient<BehaviorsGalleryViewModel>();
		services.AddTransient<ConvertersGalleryViewModel>();
		services.AddTransient<ExtensionsGalleryViewModel>();
		services.AddTransient<LayoutsGalleryViewModel>();
		services.AddTransient<ViewsGalleryViewModel>();

		// Add Alerts View Models
		services.AddTransient<SnackbarViewModel>();
		services.AddTransient<ToastViewModel>();

		// Add Behaviors View Models
		services.AddTransient<AnimationBehaviorViewModel>();
		services.AddTransient<CharactersValidationBehaviorViewModel>();
		services.AddTransient<EmailValidationBehaviorViewModel>();
		services.AddTransient<EventToCommandBehaviorViewModel>();
		services.AddTransient<MaskedBehaviorViewModel>();
		services.AddTransient<MaxLengthReachedBehaviorViewModel>();
		services.AddTransient<MultiValidationBehaviorViewModel>();
		services.AddTransient<NumericValidationBehaviorViewModel>();
		services.AddTransient<ProgressBarAnimationBehaviorViewModel>();
		services.AddTransient<RequiredStringValidationBehaviorViewModel>();
		services.AddTransient<SetFocusOnEntryCompletedBehaviorViewModel>();
		services.AddTransient<TextValidationBehaviorViewModel>();
		services.AddTransient<UriValidationBehaviorViewModel>();
		services.AddTransient<UserStoppedTypingBehaviorViewModel>();
		services.AddTransient<SelectAllTextBehaviorViewModel>();

		// Add Converters View Models
		services.AddTransient<BoolToObjectConverterViewModel>();
		services.AddTransient<ByteArrayToImageSourceConverterViewModel>();
		services.AddTransient<ColorsConverterViewModel>();
		services.AddTransient<CompareConverterViewModel>();
		services.AddTransient<DateTimeOffsetConverterViewModel>();
		services.AddTransient<DoubleToIntConverterViewModel>();
		services.AddTransient<EnumToBoolConverterViewModel>();
		services.AddTransient<EnumToIntConverterViewModel>();
		services.AddTransient<IsEqualConverterViewModel>();
		services.AddTransient<ImageResourceConverterViewModel>();
		services.AddTransient<IndexToArrayItemConverterViewModel>();
		services.AddTransient<IntToBoolConverterViewModel>();
		services.AddTransient<InvertedBoolConverterViewModel>();
		services.AddTransient<IsListNotNullOrEmptyConverterViewModel>();
		services.AddTransient<IsListNullOrEmptyConverterViewModel>();
		services.AddTransient<IsNotNullConverterViewModel>();
		services.AddTransient<IsNullConverterViewModel>();
		services.AddTransient<IsStringNotNullOrEmptyConverterViewModel>();
		services.AddTransient<IsStringNotNullOrWhiteSpaceConverterViewModel>();
		services.AddTransient<IsStringNullOrEmptyConverterViewModel>();
		services.AddTransient<IsStringNullOrWhiteSpaceConverterViewModel>();
		services.AddTransient<ItemTappedEventArgsConverterViewModel>();
		services.AddTransient<ListToStringConverterViewModel>();
		services.AddTransient<MathExpressionConverterViewModel>();
		services.AddTransient<MultiConverterViewModel>();
		services.AddTransient<MultiMathExpressionConverterViewModel>();
		services.AddTransient<IsNotEqualConverterViewModel>();
		services.AddTransient<SelectedItemEventArgsConverterViewModel>();
		services.AddTransient<StateToBooleanConverterViewModel>();
		services.AddTransient<StringToListConverterViewModel>();
		services.AddTransient<TextCaseConverterViewModel>();
		services.AddTransient<VariableMultiValueConverterViewModel>();

		// Add Extensions View Models
		services.AddTransient<ColorAnimationExtensionsViewModel>();

		// Add Layouts View Models
		services.AddTransient<UniformItemsLayoutViewModel>();

		// Add Views View Models
		services.AddTransient<CsharpBindingPopupViewModel>();
		services.AddTransient<DrawingViewViewModel>();
		services.AddTransient<MultiplePopupViewModel>();
		services.AddTransient<PopupAnchorViewModel>();
		services.AddTransient<PopupPositionViewModel>();
		services.AddTransient<XamlBindingPopupViewModel>();
	}

	static void RegisterEssentials(in IServiceCollection services)
	{
		services.AddSingleton<IDeviceInfo>(DeviceInfo.Current);
		services.AddSingleton<IDeviceDisplay>(DeviceDisplay.Current);
	}
}