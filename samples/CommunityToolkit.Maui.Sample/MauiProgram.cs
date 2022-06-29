using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Alerts;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;
using CommunityToolkit.Maui.Sample.Pages.Converters;
using CommunityToolkit.Maui.Sample.Pages.Extensions;
using CommunityToolkit.Maui.Sample.Pages.Layouts;
using CommunityToolkit.Maui.Sample.Pages.ViewControls;
using CommunityToolkit.Maui.Sample.Pages.Views;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.Layouts;
using CommunityToolkit.Maui.Sample.ViewModels.ViewControls;
using CommunityToolkit.Maui.Sample.ViewModels.Views;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace CommunityToolkit.Maui.Sample;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		MauiAppBuilder builder = MauiApp.CreateBuilder()
								.UseMauiApp<App>()
								.UseMauiCommunityToolkit()
								.UseMauiCommunityToolkitMarkup();

		_ = builder.Services.AddHttpClient<ByteArrayToImageSourceConverterViewModel>();
		_ = builder.Services.AddSingleton<PopupSizeConstants>();

		RegisterPages(builder.Services);
		RegisterViewModels(builder.Services);
		RegisterEssentials(builder.Services);

		return builder.Build();
	}

	static void RegisterPages(in IServiceCollection services)
	{
		// Add Gallery Pages
		_ = services.AddTransient<AlertsGalleryPage>();
		_ = services.AddTransient<BehaviorsGalleryPage>();
		_ = services.AddTransient<ControlsGalleryPage>();
		_ = services.AddTransient<ConvertersGalleryPage>();
		_ = services.AddTransient<ExtensionsGalleryPage>();
		_ = services.AddTransient<LayoutsGalleryPage>();
		_ = services.AddTransient<ViewsGalleryPage>();

		// Add Alerts Pages
		_ = services.AddTransient<SnackbarPage>();
		_ = services.AddTransient<ToastPage>();

		// Add Behaviors Pages
		_ = services.AddTransient<AnimationBehaviorPage>();
		_ = services.AddTransient<CharactersValidationBehaviorPage>();
		_ = services.AddTransient<EmailValidationBehaviorPage>();
		_ = services.AddTransient<EventToCommandBehaviorPage>();
		_ = services.AddTransient<MaskedBehaviorPage>();
		_ = services.AddTransient<MaxLengthReachedBehaviorPage>();
		_ = services.AddTransient<MultiValidationBehaviorPage>();
		_ = services.AddTransient<NumericValidationBehaviorPage>();
		_ = services.AddTransient<ProgressBarAnimationBehaviorPage>();
		_ = services.AddTransient<RequiredStringValidationBehaviorPage>();
		_ = services.AddTransient<SelectAllTextBehaviorPage>();
		_ = services.AddTransient<SetFocusOnEntryCompletedBehaviorPage>();
		_ = services.AddTransient<TextValidationBehaviorPage>();
		_ = services.AddTransient<UriValidationBehaviorPage>();
		_ = services.AddTransient<UserStoppedTypingBehaviorPage>();

		// Add Converters Pages
		_ = services.AddTransient<BoolToObjectConverterPage>();
		_ = services.AddTransient<ByteArrayToImageSourceConverterPage>();
		_ = services.AddTransient<ColorsConverterPage>();
		_ = services.AddTransient<CompareConverterPage>();
		_ = services.AddTransient<DateTimeOffsetConverterPage>();
		_ = services.AddTransient<DoubleToIntConverterPage>();
		_ = services.AddTransient<EnumToBoolConverterPage>();
		_ = services.AddTransient<EnumToIntConverterPage>();
		_ = services.AddTransient<ImageResourceConverterPage>();
		_ = services.AddTransient<IndexToArrayItemConverterPage>();
		_ = services.AddTransient<IntToBoolConverterPage>();
		_ = services.AddTransient<InvertedBoolConverterPage>();
		_ = services.AddTransient<IsEqualConverterPage>();
		_ = services.AddTransient<IsListNotNullOrEmptyConverterPage>();
		_ = services.AddTransient<IsListNullOrEmptyConverterPage>();
		_ = services.AddTransient<IsNotEqualConverterPage>();
		_ = services.AddTransient<IsNotNullConverterPage>();
		_ = services.AddTransient<IsNullConverterPage>();
		_ = services.AddTransient<IsStringNotNullOrEmptyConverterPage>();
		_ = services.AddTransient<IsStringNotNullOrWhiteSpaceConverterPage>();
		_ = services.AddTransient<IsStringNullOrEmptyConverterPage>();
		_ = services.AddTransient<IsStringNullOrWhiteSpaceConverterPage>();
		_ = services.AddTransient<ItemTappedEventArgsConverterPage>();
		_ = services.AddTransient<ListToStringConverterPage>();
		_ = services.AddTransient<MathExpressionConverterPage>();
		_ = services.AddTransient<MultiConverterPage>();
		_ = services.AddTransient<MultiMathExpressionConverterPage>();
		_ = services.AddTransient<SelectedItemEventArgsConverterPage>();
		_ = services.AddTransient<StateToBooleanConverterPage>();
		_ = services.AddTransient<StringToListConverterPage>();
		_ = services.AddTransient<TextCaseConverterPage>();
		_ = services.AddTransient<VariableMultiValueConverterPage>();

		// Add Extensions Pages
		_ = services.AddTransient<ColorAnimationExtensionsPage>();

		// Add Layouts Pages
		_ = services.AddTransient<UniformItemsLayoutPage>();

		// Add Views Pages
		_ = services.AddTransient<CsharpBindingPopup>();
		_ = services.AddTransient<DrawingViewPage>();
		_ = services.AddTransient<MultiplePopupPage>();
		_ = services.AddTransient<PopupAnchorPage>();
		_ = services.AddTransient<PopupPositionPage>();
		_ = services.AddTransient<XamlBindingPopup>();

		// Add Controls Pages
		_ = services.AddTransient<AvatarControlPage>();
	}

	static void RegisterViewModels(in IServiceCollection services)
	{
		// Add Gallery View Models
		_ = services.AddTransient<AlertsGalleryViewModel>();
		_ = services.AddTransient<BehaviorsGalleryViewModel>();
		_ = services.AddTransient<ControlsGalleryViewModel>();
		_ = services.AddTransient<ConvertersGalleryViewModel>();
		_ = services.AddTransient<ExtensionsGalleryViewModel>();
		_ = services.AddTransient<LayoutsGalleryViewModel>();
		_ = services.AddTransient<ViewsGalleryViewModel>();

		// Add Alerts View Models
		_ = services.AddTransient<SnackbarViewModel>();
		_ = services.AddTransient<ToastViewModel>();

		// Add Behaviors View Models
		_ = services.AddTransient<AnimationBehaviorViewModel>();
		_ = services.AddTransient<CharactersValidationBehaviorViewModel>();
		_ = services.AddTransient<EmailValidationBehaviorViewModel>();
		_ = services.AddTransient<EventToCommandBehaviorViewModel>();
		_ = services.AddTransient<MaskedBehaviorViewModel>();
		_ = services.AddTransient<MaxLengthReachedBehaviorViewModel>();
		_ = services.AddTransient<MultiValidationBehaviorViewModel>();
		_ = services.AddTransient<NumericValidationBehaviorViewModel>();
		_ = services.AddTransient<ProgressBarAnimationBehaviorViewModel>();
		_ = services.AddTransient<RequiredStringValidationBehaviorViewModel>();
		_ = services.AddTransient<SelectAllTextBehaviorViewModel>();
		_ = services.AddTransient<SetFocusOnEntryCompletedBehaviorViewModel>();
		_ = services.AddTransient<TextValidationBehaviorViewModel>();
		_ = services.AddTransient<UriValidationBehaviorViewModel>();
		_ = services.AddTransient<UserStoppedTypingBehaviorViewModel>();

		// Add Converters View Models
		_ = services.AddTransient<BoolToObjectConverterViewModel>();
		_ = services.AddTransient<ByteArrayToImageSourceConverterViewModel>();
		_ = services.AddTransient<ColorsConverterViewModel>();
		_ = services.AddTransient<CompareConverterViewModel>();
		_ = services.AddTransient<DateTimeOffsetConverterViewModel>();
		_ = services.AddTransient<DoubleToIntConverterViewModel>();
		_ = services.AddTransient<EnumToBoolConverterViewModel>();
		_ = services.AddTransient<EnumToIntConverterViewModel>();
		_ = services.AddTransient<IsEqualConverterViewModel>();
		_ = services.AddTransient<ImageResourceConverterViewModel>();
		_ = services.AddTransient<IndexToArrayItemConverterViewModel>();
		_ = services.AddTransient<IntToBoolConverterViewModel>();
		_ = services.AddTransient<InvertedBoolConverterViewModel>();
		_ = services.AddTransient<IsListNotNullOrEmptyConverterViewModel>();
		_ = services.AddTransient<IsListNullOrEmptyConverterViewModel>();
		_ = services.AddTransient<IsNotNullConverterViewModel>();
		_ = services.AddTransient<IsNullConverterViewModel>();
		_ = services.AddTransient<IsStringNotNullOrEmptyConverterViewModel>();
		_ = services.AddTransient<IsStringNotNullOrWhiteSpaceConverterViewModel>();
		_ = services.AddTransient<IsStringNullOrEmptyConverterViewModel>();
		_ = services.AddTransient<IsStringNullOrWhiteSpaceConverterViewModel>();
		_ = services.AddTransient<ItemTappedEventArgsConverterViewModel>();
		_ = services.AddTransient<ListToStringConverterViewModel>();
		_ = services.AddTransient<MathExpressionConverterViewModel>();
		_ = services.AddTransient<MultiConverterViewModel>();
		_ = services.AddTransient<MultiMathExpressionConverterViewModel>();
		_ = services.AddTransient<IsNotEqualConverterViewModel>();
		_ = services.AddTransient<SelectedItemEventArgsConverterViewModel>();
		_ = services.AddTransient<StateToBooleanConverterViewModel>();
		_ = services.AddTransient<StringToListConverterViewModel>();
		_ = services.AddTransient<TextCaseConverterViewModel>();
		_ = services.AddTransient<VariableMultiValueConverterViewModel>();

		// Add Extensions View Models
		_ = services.AddTransient<ColorAnimationExtensionsViewModel>();

		// Add Layouts View Models
		_ = services.AddTransient<UniformItemsLayoutViewModel>();

		// Add Views View Models
		_ = services.AddTransient<CsharpBindingPopupViewModel>();
		_ = services.AddTransient<DrawingViewViewModel>();
		_ = services.AddTransient<MultiplePopupViewModel>();
		_ = services.AddTransient<PopupAnchorViewModel>();
		_ = services.AddTransient<PopupPositionViewModel>();
		_ = services.AddTransient<XamlBindingPopupViewModel>();

		// Add Controls View Models
		_ = services.AddTransient<AvatarControlViewModel>();
	}

	static void RegisterEssentials(in IServiceCollection services)
	{
		_ = services.AddSingleton<IDeviceInfo>(DeviceInfo.Current);
		_ = services.AddSingleton<IDeviceDisplay>(DeviceDisplay.Current);
	}
}