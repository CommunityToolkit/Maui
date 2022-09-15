using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages;
using CommunityToolkit.Maui.Sample.Pages.Alerts;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;
using CommunityToolkit.Maui.Sample.Pages.Converters;
using CommunityToolkit.Maui.Sample.Pages.Extensions;
using CommunityToolkit.Maui.Sample.Pages.Layouts;
using CommunityToolkit.Maui.Sample.Pages.Views;
using CommunityToolkit.Maui.Sample.ViewModels;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.Layouts;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Sample.ViewModels.Views.AvatarView;
using Polly;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace CommunityToolkit.Maui.Sample;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder()
#if DEBUG
								.UseMauiCommunityToolkit()
#else
								.UseMauiCommunityToolkit(options =>
								{
									options.SetShouldSuppressExceptionsInConverters(false);
									options.SetShouldSuppressExceptionsInBehaviors(false);
									options.SetShouldSuppressExceptionsInAnimations(false);
								})
#endif
								.UseMauiCommunityToolkitMarkup()
								.UseMauiApp<App>();

		builder.Services.AddHttpClient<ByteArrayToImageSourceConverterViewModel>()
						.AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(3, sleepDurationProvider));

		builder.Services.AddSingleton<PopupSizeConstants>();

		RegisterViewsAndViewModels(builder.Services);
		RegisterEssentials(builder.Services);

		return builder.Build();

		static TimeSpan sleepDurationProvider(int attemptNumber) => TimeSpan.FromSeconds(Math.Pow(2, attemptNumber));
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
		services.AddTransientWithShellRoute<SnackbarPage, SnackbarViewModel>();
		services.AddTransientWithShellRoute<ToastPage, ToastViewModel>();

		// Add AvatarView pages + ViewModels
		services.AddTransientWithShellRoute<AvatarViewBindablePropertiesPage, AvatarViewBindablePropertiesViewModel>();
		services.AddTransientWithShellRoute<AvatarViewBordersPage, AvatarViewBordersViewModel>();
		services.AddTransientWithShellRoute<AvatarViewColorsPage, AvatarViewColorsViewModel>();
		services.AddTransientWithShellRoute<AvatarViewDayOfWeekPage, AvatarViewDayOfWeekViewModel>();
		services.AddTransientWithShellRoute<AvatarViewGesturesPage, AvatarViewGesturesViewModel>();
		services.AddTransientWithShellRoute<AvatarViewImagesPage, AvatarViewImagesViewModel>();
		services.AddTransientWithShellRoute<AvatarViewKeyboardPage, AvatarViewKeyboardViewModel>();
		services.AddTransientWithShellRoute<AvatarViewRatingPage, AvatarViewRatingViewModel>();
		services.AddTransientWithShellRoute<AvatarViewShadowsPage, AvatarViewShadowsViewModel>();
		services.AddTransientWithShellRoute<AvatarViewShapesPage, AvatarViewShapesViewModel>();
		services.AddTransientWithShellRoute<AvatarViewSizesPage, AvatarViewSizesViewModel>();

		// Add Behaviors Pages + ViewModels
		services.AddTransientWithShellRoute<AnimationBehaviorPage, AnimationBehaviorViewModel>();
		services.AddTransientWithShellRoute<CharactersValidationBehaviorPage, CharactersValidationBehaviorViewModel>();
		services.AddTransientWithShellRoute<EmailValidationBehaviorPage, EmailValidationBehaviorViewModel>();
		services.AddTransientWithShellRoute<EventToCommandBehaviorPage, EventToCommandBehaviorViewModel>();
		services.AddTransientWithShellRoute<IconTintColorBehaviorPage, IconTintColorBehaviorViewModel>();
		services.AddTransientWithShellRoute<MaskedBehaviorPage, MaskedBehaviorViewModel>();
		services.AddTransientWithShellRoute<MaxLengthReachedBehaviorPage, MaxLengthReachedBehaviorViewModel>();
		services.AddTransientWithShellRoute<MultiValidationBehaviorPage, MultiValidationBehaviorViewModel>();
		services.AddTransientWithShellRoute<NumericValidationBehaviorPage, NumericValidationBehaviorViewModel>();
		services.AddTransientWithShellRoute<ProgressBarAnimationBehaviorPage, ProgressBarAnimationBehaviorViewModel>();
		services.AddTransientWithShellRoute<RequiredStringValidationBehaviorPage, RequiredStringValidationBehaviorViewModel>();
		services.AddTransientWithShellRoute<SelectAllTextBehaviorPage, SelectAllTextBehaviorViewModel>();
		services.AddTransientWithShellRoute<SetFocusOnEntryCompletedBehaviorPage, SetFocusOnEntryCompletedBehaviorViewModel>();
		services.AddTransientWithShellRoute<TextValidationBehaviorPage, TextValidationBehaviorViewModel>();
		services.AddTransientWithShellRoute<UriValidationBehaviorPage, UriValidationBehaviorViewModel>();
		services.AddTransientWithShellRoute<UserStoppedTypingBehaviorPage, UserStoppedTypingBehaviorViewModel>();
		services.AddTransientWithShellRoute<StatusBarBehaviorPage, StatusBarBehaviorViewModel>();

		// Add Converters Pages + ViewModels
		services.AddTransientWithShellRoute<BoolToObjectConverterPage, BoolToObjectConverterViewModel>();
		services.AddTransientWithShellRoute<ByteArrayToImageSourceConverterPage, ByteArrayToImageSourceConverterViewModel>();
		services.AddTransientWithShellRoute<ColorsConverterPage, ColorsConverterViewModel>();
		services.AddTransientWithShellRoute<CompareConverterPage, CompareConverterViewModel>();
		services.AddTransientWithShellRoute<DateTimeOffsetConverterPage, DateTimeOffsetConverterViewModel>();
		services.AddTransientWithShellRoute<DoubleToIntConverterPage, DoubleToIntConverterViewModel>();
		services.AddTransientWithShellRoute<EnumToBoolConverterPage, EnumToBoolConverterViewModel>();
		services.AddTransientWithShellRoute<EnumToIntConverterPage, EnumToIntConverterViewModel>();
		services.AddTransientWithShellRoute<ImageResourceConverterPage, ImageResourceConverterViewModel>();
		services.AddTransientWithShellRoute<IndexToArrayItemConverterPage, IndexToArrayItemConverterViewModel>();
		services.AddTransientWithShellRoute<IntToBoolConverterPage, IntToBoolConverterViewModel>();
		services.AddTransientWithShellRoute<InvertedBoolConverterPage, InvertedBoolConverterViewModel>();
		services.AddTransientWithShellRoute<IsEqualConverterPage, IsEqualConverterViewModel>();
		services.AddTransientWithShellRoute<IsListNotNullOrEmptyConverterPage, IsListNotNullOrEmptyConverterViewModel>();
		services.AddTransientWithShellRoute<IsListNullOrEmptyConverterPage, IsListNullOrEmptyConverterViewModel>();
		services.AddTransientWithShellRoute<IsNotEqualConverterPage, IsNotEqualConverterViewModel>();
		services.AddTransientWithShellRoute<IsNotNullConverterPage, IsNotNullConverterViewModel>();
		services.AddTransientWithShellRoute<IsNullConverterPage, IsNullConverterViewModel>();
		services.AddTransientWithShellRoute<IsStringNotNullOrEmptyConverterPage, IsStringNotNullOrEmptyConverterViewModel>();
		services.AddTransientWithShellRoute<IsStringNotNullOrWhiteSpaceConverterPage, IsStringNotNullOrWhiteSpaceConverterViewModel>();
		services.AddTransientWithShellRoute<IsStringNullOrEmptyConverterPage, IsStringNullOrEmptyConverterViewModel>();
		services.AddTransientWithShellRoute<IsStringNullOrWhiteSpaceConverterPage, IsStringNullOrWhiteSpaceConverterViewModel>();
		services.AddTransientWithShellRoute<ItemTappedEventArgsConverterPage, ItemTappedEventArgsConverterViewModel>();
		services.AddTransientWithShellRoute<ListToStringConverterPage, ListToStringConverterViewModel>();
		services.AddTransientWithShellRoute<MathExpressionConverterPage, MathExpressionConverterViewModel>();
		services.AddTransientWithShellRoute<MultiConverterPage, MultiConverterViewModel>();
		services.AddTransientWithShellRoute<MultiMathExpressionConverterPage, MultiMathExpressionConverterViewModel>();
		services.AddTransientWithShellRoute<SelectedItemEventArgsConverterPage, SelectedItemEventArgsConverterViewModel>();
		services.AddTransientWithShellRoute<StateToBooleanConverterPage, StateToBooleanConverterViewModel>();
		services.AddTransientWithShellRoute<StringToListConverterPage, StringToListConverterViewModel>();
		services.AddTransientWithShellRoute<TextCaseConverterPage, TextCaseConverterViewModel>();
		services.AddTransientWithShellRoute<VariableMultiValueConverterPage, VariableMultiValueConverterViewModel>();

		// Add Extensions Pages + ViewModels
		services.AddTransientWithShellRoute<ColorAnimationExtensionsPage, ColorAnimationExtensionsViewModel>();

		// Add Layouts Pages + ViewModels
		services.AddTransientWithShellRoute<UniformItemsLayoutPage, UniformItemsLayoutViewModel>();

		// Add Views Pages + ViewModels
		services.AddTransientWithShellRoute<DrawingViewPage, DrawingViewViewModel>();
		services.AddTransientWithShellRoute<MultiplePopupPage, MultiplePopupViewModel>();
		services.AddTransientWithShellRoute<PopupAnchorPage, PopupAnchorViewModel>();
		services.AddTransientWithShellRoute<PopupPositionPage, PopupPositionViewModel>();

		// Add Popups
		services.AddTransient<CsharpBindingPopup, CsharpBindingPopupViewModel>();
		services.AddTransient<XamlBindingPopup, XamlBindingPopupViewModel>();
	}

	static void RegisterEssentials(in IServiceCollection services)
	{
		services.AddSingleton<IDeviceInfo>(DeviceInfo.Current);
		services.AddSingleton<IDeviceDisplay>(DeviceDisplay.Current);
	}

	static IServiceCollection AddTransientWithShellRoute<TPage, TViewModel>(this IServiceCollection services) where TPage : BasePage<TViewModel>
																												where TViewModel : BaseViewModel
	{
		return services.AddTransientWithShellRoute<TPage, TViewModel>(AppShell.GetPageRoute<TViewModel>());
	}
}