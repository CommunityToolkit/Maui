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
		// Add Gallery Pages + ViewModels
		services.AddTransient<AlertsGalleryPage, AlertsGalleryViewModel>();
		services.AddTransient<BehaviorsGalleryPage, BehaviorsGalleryViewModel>();
		services.AddTransient<ConvertersGalleryPage, ConvertersGalleryViewModel>();
		services.AddTransient<ExtensionsGalleryPage, ExtensionsGalleryViewModel>();
		services.AddTransient<LayoutsGalleryPage, LayoutsGalleryViewModel>();
		services.AddTransient<ViewsGalleryPage, ViewsGalleryViewModel>();

		// Add Alerts Pages + ViewModels
		services.AddTransient<SnackbarPage, SnackbarViewModel>();
		services.AddTransient<ToastPage, ToastViewModel>();

		// Add AvatarView pages + ViewModels
		services.AddTransient<AvatarViewBindablePropertiesPage, AvatarViewBindablePropertiesViewModel>();
		services.AddTransient<AvatarViewBordersPage, AvatarViewBordersViewModel>();
		services.AddTransient<AvatarViewColorsPage, AvatarViewColorsViewModel>();
		services.AddTransient<AvatarViewDayOfWeekPage, AvatarViewDayOfWeekViewModel>();
		services.AddTransient<AvatarViewGesturesPage, AvatarViewGesturesViewModel>();
		services.AddTransient<AvatarViewImagesPage, AvatarViewImagesViewModel>();
		services.AddTransient<AvatarViewKeyboardPage, AvatarViewKeyboardViewModel>();
		services.AddTransient<AvatarViewRatingPage, AvatarViewRatingViewModel>();
		services.AddTransient<AvatarViewShadowsPage, AvatarViewShadowsViewModel>();
		services.AddTransient<AvatarViewShapesPage, AvatarViewShapesViewModel>();
		services.AddTransient<AvatarViewSizesPage, AvatarViewSizesViewModel>();

		// Add Behaviors Pages + ViewModels
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

		// Add Converters Pages + ViewModels
		services.AddTransient<BoolToObjectConverterPage, BoolToObjectConverterViewModel>();
		services.AddTransient<ByteArrayToImageSourceConverterPage, ByteArrayToImageSourceConverterViewModel>();
		services.AddTransient<ColorsConverterPage, ColorsConverterViewModel>();
		services.AddTransient<CompareConverterPage, CompareConverterViewModel>();
		services.AddTransient<DateTimeOffsetConverterPage, DateTimeOffsetConverterViewModel>();
		services.AddTransient<DoubleToIntConverterPage, DoubleToIntConverterViewModel>();
		services.AddTransient<EnumToBoolConverterPage, EnumToBoolConverterViewModel>();
		services.AddTransient<EnumToIntConverterPage, EnumToIntConverterViewModel>();
		services.AddTransient<ImageResourceConverterPage, ImageResourceConverterViewModel>();
		services.AddTransient<IndexToArrayItemConverterPage, IndexToArrayItemConverterViewModel>();
		services.AddTransient<IntToBoolConverterPage, IntToBoolConverterViewModel>();
		services.AddTransient<InvertedBoolConverterPage, InvertedBoolConverterViewModel>();
		services.AddTransient<IsEqualConverterPage, IsEqualConverterViewModel>();
		services.AddTransient<IsListNotNullOrEmptyConverterPage, IsListNotNullOrEmptyConverterViewModel>();
		services.AddTransient<IsListNullOrEmptyConverterPage, IsListNullOrEmptyConverterViewModel>();
		services.AddTransient<IsNotEqualConverterPage, IsNotEqualConverterViewModel>();
		services.AddTransient<IsNotNullConverterPage, IsNotNullConverterViewModel>();
		services.AddTransient<IsNullConverterPage, IsNullConverterViewModel>();
		services.AddTransient<IsStringNotNullOrEmptyConverterPage, IsStringNotNullOrEmptyConverterViewModel>();
		services.AddTransient<IsStringNotNullOrWhiteSpaceConverterPage, IsStringNotNullOrWhiteSpaceConverterViewModel>();
		services.AddTransient<IsStringNullOrEmptyConverterPage, IsStringNullOrEmptyConverterViewModel>();
		services.AddTransient<IsStringNullOrWhiteSpaceConverterPage, IsStringNullOrWhiteSpaceConverterViewModel>();
		services.AddTransient<ItemTappedEventArgsConverterPage, ItemTappedEventArgsConverterViewModel>();
		services.AddTransient<ListToStringConverterPage, ListToStringConverterViewModel>();
		services.AddTransient<MathExpressionConverterPage, MathExpressionConverterViewModel>();
		services.AddTransient<MultiConverterPage, MultiConverterViewModel>();
		services.AddTransient<MultiMathExpressionConverterPage, MultiMathExpressionConverterViewModel>();
		services.AddTransient<SelectedItemEventArgsConverterPage, SelectedItemEventArgsConverterViewModel>();
		services.AddTransient<StateToBooleanConverterPage, StateToBooleanConverterViewModel>();
		services.AddTransient<StringToListConverterPage, StringToListConverterViewModel>();
		services.AddTransient<TextCaseConverterPage, TextCaseConverterViewModel>();
		services.AddTransient<VariableMultiValueConverterPage, VariableMultiValueConverterViewModel>();

		// Add Extensions Pages + ViewModels
		services.AddTransient<ColorAnimationExtensionsPage, ColorAnimationExtensionsViewModel>();

		// Add Layouts Pages + ViewModels
		services.AddTransient<UniformItemsLayoutPage, UniformItemsLayoutViewModel>();

		// Add Views Pages + ViewModels
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