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
using CommunityToolkit.Maui.Sample.ViewModels.Views.AvatarView;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace CommunityToolkit.Maui.Sample;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder()
								.UseMauiCommunityToolkit()
								.UseMauiCommunityToolkitMarkup()
								.UseMauiApp<App>();

		builder.Services.AddHttpClient<ByteArrayToImageSourceConverterViewModel>();
		builder.Services.AddSingleton<PopupSizeConstants>();

		RegisterViewsAndViewModels(builder.Services);
		RegisterEssentials(builder.Services);

		return builder.Build();
	}

	static void RegisterViewsAndViewModels(in IServiceCollection services)
	{
		// Add Gallery Pages / ViewModels
		services.AddTransient<AlertsGalleryPage, AlertsGalleryViewModel>();
		services.AddTransient<BehaviorsGalleryPage, BehaviorsGalleryViewModel>();
		services.AddTransient<ConvertersGalleryPage, ConvertersGalleryViewModel>();
		services.AddTransient<ExtensionsGalleryPage, ExtensionsGalleryViewModel>();
		services.AddTransient<LayoutsGalleryPage, LayoutsGalleryViewModel>();
		services.AddTransient<ViewsGalleryPage, ViewsGalleryViewModel>();

		// Add Alerts Pages / ViewModels
		services.AddTransient<SnackbarPage, SnackbarViewModel>();
		services.AddTransient<ToastPage, ToastViewModel>();

		// Add Behaviors Pages / ViewModels
		services.AddTransient<AnimationBehaviorPage, AnimationBehaviorViewModel>();
		services.AddTransient<CharactersValidationBehaviorPage, CharactersValidationBehaviorViewModel>();
		services.AddTransient<EmailValidationBehaviorPage, EmailValidationBehaviorViewModel>();
		services.AddTransient<EventToCommandBehaviorPage, EventToCommandBehaviorViewModel>();
		services.AddTransient<IconTintColorBehaviorPage, IconTintColorBehaviorViewModel>();
		services.AddTransient<MaskedBehaviorPage, MaskedBehaviorViewModel>();
		services.AddTransient<MaxLengthReachedBehaviorPage, MaxLengthReachedBehaviorViewModel>();
		services.AddTransient<MultiValidationBehaviorPage, MultiValidationBehaviorViewModel>();
		services.AddTransient<NumericValidationBehaviorPage, NumericValidationBehaviorViewModel>();
		services.AddTransient<ProgressBarAnimationBehaviorPage, ProgressBarAnimationBehaviorViewModel>();
		services.AddTransient<RequiredStringValidationBehaviorPage, RequiredStringValidationBehaviorViewModel>();
		services.AddTransient<SelectAllTextBehaviorPage, SelectAllTextBehaviorViewModel>();
		services.AddTransient<SetFocusOnEntryCompletedBehaviorPage, SetFocusOnEntryCompletedBehaviorViewModel>();
		services.AddTransient<TextValidationBehaviorPage, TextValidationBehaviorViewModel>();
		services.AddTransient<UriValidationBehaviorPage, UriValidationBehaviorViewModel>();
		services.AddTransient<UserStoppedTypingBehaviorPage, UserStoppedTypingBehaviorViewModel>();

		// Add Converters Pages
		services.AddTransient<BoolToObjectConverterPage>();
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
		services.AddTransient<StateToBooleanConverterPage>();
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
		services.AddTransient<IconTintColorBehaviorViewModel>();
		services.AddTransient<MaskedBehaviorViewModel>();
		services.AddTransient<MaxLengthReachedBehaviorViewModel>();
		services.AddTransient<MultiValidationBehaviorViewModel>();
		services.AddTransient<NumericValidationBehaviorViewModel>();
		services.AddTransient<ProgressBarAnimationBehaviorViewModel>();
		services.AddTransient<RequiredStringValidationBehaviorViewModel>();
		services.AddTransient<SelectAllTextBehaviorViewModel>();
		services.AddTransient<SetFocusOnEntryCompletedBehaviorViewModel>();
		services.AddTransient<TextValidationBehaviorViewModel>();
		services.AddTransient<UriValidationBehaviorViewModel>();
		services.AddTransient<UserStoppedTypingBehaviorViewModel>();

		// Add Extensions Pages / ViewModels
		services.AddTransient<ColorAnimationExtensionsPage, ColorAnimationExtensionsViewModel>();

		// Add Layouts Pages / ViewModels
		services.AddTransient<UniformItemsLayoutPage, UniformItemsLayoutViewModel>();

		// Add Views Pages / ViewModels
		services.AddTransient<DrawingViewPage, DrawingViewViewModel>();
		services.AddTransient<MultiplePopupPage, MultiplePopupViewModel>();
		services.AddTransient<PopupAnchorPage, PopupAnchorViewModel>();
		services.AddTransient<PopupPositionPage, PopupPositionViewModel>();

		// Add Popups
		services.AddTransient<CsharpBindingPopup, CsharpBindingPopupViewModel>();
		services.AddTransient<XamlBindingPopup, XamlBindingPopupViewModel>();
	}

	static void RegisterEssentials(in IServiceCollection services)
	{
		services.AddSingleton<IDeviceInfo>(DeviceInfo.Current);
		services.AddSingleton<IDeviceDisplay>(DeviceDisplay.Current);
	}
}