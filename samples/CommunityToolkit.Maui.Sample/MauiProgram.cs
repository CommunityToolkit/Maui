using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.ApplicationModel;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages;
using CommunityToolkit.Maui.Sample.Pages.Alerts;
using CommunityToolkit.Maui.Sample.Pages.Behaviors;
using CommunityToolkit.Maui.Sample.Pages.Converters;
using CommunityToolkit.Maui.Sample.Pages.Essentials;
using CommunityToolkit.Maui.Sample.Pages.Extensions;
using CommunityToolkit.Maui.Sample.Pages.ImageSources;
using CommunityToolkit.Maui.Sample.Pages.Layouts;
using CommunityToolkit.Maui.Sample.Pages.PlatformSpecific;
using CommunityToolkit.Maui.Sample.Pages.Views;
using CommunityToolkit.Maui.Sample.Resources.Fonts;
using CommunityToolkit.Maui.Sample.ViewModels;
using CommunityToolkit.Maui.Sample.ViewModels.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.Essentials;
using CommunityToolkit.Maui.Sample.ViewModels.Extensions;
using CommunityToolkit.Maui.Sample.ViewModels.ImageSources;
using CommunityToolkit.Maui.Sample.ViewModels.Layouts;
using CommunityToolkit.Maui.Sample.ViewModels.PlatformSpecific;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Sample.ViewModels.Views.AvatarView;
using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Polly;

#if WINDOWS10_0_17763_0_OR_GREATER
using CommunityToolkit.Maui.Maps;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;
using Microsoft.Maui.Platform;
#endif

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace CommunityToolkit.Maui.Sample;

public static class MauiProgram
{
	[RequiresUnreferencedCode($"{nameof(CommunityToolkit.Maui.Views.Expander)} and  {nameof(TouchBehaviorCollectionViewMultipleSelectionPage)} are not type safe")]
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder()
#if DEBUG
								.UseMauiCommunityToolkit(static options =>
								{
									options.SetShouldEnableSnackbarOnWindows(true);
								})
#else
								.UseMauiCommunityToolkit(static options =>
								{
									options.SetShouldEnableSnackbarOnWindows(true);
									options.SetShouldSuppressExceptionsInConverters(true);
									options.SetShouldSuppressExceptionsInBehaviors(true);
									options.SetShouldSuppressExceptionsInAnimations(true);
								})
#endif
								.UseMauiCommunityToolkitMarkup()
								.UseMauiCommunityToolkitCamera()
								.UseMauiCommunityToolkitMediaElement()

								.ConfigureMauiHandlers(handlers =>
								{
#if IOS || MACCATALYST
									handlers.AddHandler<CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
									handlers.AddHandler<CarouselView, Microsoft.Maui.Controls.Handlers.Items2.CarouselViewHandler2>();
#endif
								})

#if WINDOWS
								.UseMauiCommunityToolkitMaps("KEY") // You should add your own key here from https://bingmapsportal.com
#else
								.UseMauiMaps()
#endif

								.UseMauiApp<App>()
								.ConfigureFonts(static fonts =>
								{
									fonts.AddFont("Font Awesome 6 Brands-Regular-400.otf", FontFamilies.FontAwesomeBrands);
								});

		builder.ConfigureLifecycleEvents(events =>
		{
#if WINDOWS10_0_17763_0_OR_GREATER
				events.AddWindows(static windowLifeCycleBuilder =>
				{
					windowLifeCycleBuilder.OnWindowCreated(window =>
					{
						window.SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };

						var titleBar = window.GetAppWindow()?.TitleBar ?? throw new InvalidOperationException("App Window Cannot be Null");

						titleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

						window.ExtendsContentIntoTitleBar = false;

						IntPtr nativeWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
						WindowId win32WindowsId = Win32Interop.GetWindowIdFromWindow(nativeWindowHandle);
						AppWindow winuiAppWindow = AppWindow.GetFromWindowId(win32WindowsId);

						if (winuiAppWindow.Presenter is OverlappedPresenter p)
						{
							p.SetBorderAndTitleBar(true, true);
						}
					});
				});
#endif
		});

		builder.Services.AddHttpClient<ByteArrayToImageSourceConverterViewModel>()
						.AddStandardResilienceHandler(static options => options.Retry = new MobileHttpRetryStrategyOptions());

		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddSingleton<PopupSizeConstants>();

		RegisterViewsAndViewModels(builder.Services);
		RegisterEssentials(builder.Services);

#if DEBUG
		builder.Logging.AddDebug().SetMinimumLevel(LogLevel.Trace);
#endif

		return builder.Build();
	}

	[RequiresUnreferencedCode("Calls CommunityToolkit.Maui.Sample.MauiProgram.AddTransientWithShellRoute<TPage, TViewModel>()")]
	static void RegisterViewsAndViewModels(in IServiceCollection services)
	{
		// Add Gallery Pages + ViewModels
		services.AddTransient<AlertsGalleryPage, AlertsGalleryViewModel>();
		services.AddTransient<BehaviorsGalleryPage, BehaviorsGalleryViewModel>();
		services.AddTransient<ConvertersGalleryPage, ConvertersGalleryViewModel>();
		services.AddTransient<EssentialsGalleryPage, EssentialsGalleryViewModel>();
		services.AddTransient<ExtensionsGalleryPage, ExtensionsGalleryViewModel>();
		services.AddTransient<ImageSourcesGalleryPage, ImageSourcesGalleryViewModel>();
		services.AddTransient<LayoutsGalleryPage, LayoutsGalleryViewModel>();
		services.AddTransient<ViewsGalleryPage, ViewsGalleryViewModel>();
		services.AddTransient<PlatformSpecificGalleryPage, PlatformSpecificGalleryViewModel>();

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
		services.AddTransientWithShellRoute<StatusBarBehaviorPage, StatusBarBehaviorViewModel>();
		services.AddTransientWithShellRoute<TextValidationBehaviorPage, TextValidationBehaviorViewModel>();
		services.AddTransientWithShellRoute<TouchBehaviorPage, TouchBehaviorViewModel>();
		services.AddTransientWithShellRoute<TouchBehaviorCollectionViewMultipleSelectionPage, TouchBehaviorCollectionViewMultipleSelectionViewModel>();
		services.AddTransientWithShellRoute<UriValidationBehaviorPage, UriValidationBehaviorViewModel>();
		services.AddTransientWithShellRoute<UserStoppedTypingBehaviorPage, UserStoppedTypingBehaviorViewModel>();

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
		services.AddTransientWithShellRoute<IsInRangeConverterPage, IsInRangeConverterViewModel>();
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

		// Add Essentials Pages + ViewModels
		services.AddTransientWithShellRoute<AppThemePage, AppThemeViewModel>();
		services.AddTransientWithShellRoute<BadgePage, BadgeViewModel>();
		services.AddTransientWithShellRoute<FileSaverPage, FileSaverViewModel>();
		services.AddTransientWithShellRoute<FolderPickerPage, FolderPickerViewModel>();
		services.AddTransientWithShellRoute<SpeechToTextPage, SpeechToTextViewModel>();
		services.AddTransientWithShellRoute<OfflineSpeechToTextPage, OfflineSpeechToTextViewModel>();

		// Add Extensions Pages + ViewModels
		services.AddTransientWithShellRoute<ColorAnimationExtensionsPage, ColorAnimationExtensionsViewModel>();
		services.AddTransientWithShellRoute<KeyboardExtensionsPage, KeyboardExtensionsViewModel>();

		// Add ImageSources Pages + ViewModels
		services.AddTransientWithShellRoute<GravatarImageSourcePage, GravatarImageSourceViewModel>();

		// Add Layouts Pages + ViewModels
		services.AddTransientWithShellRoute<DockLayoutPage, DockLayoutViewModel>();
		services.AddTransientWithShellRoute<StateContainerPage, StateContainerViewModel>();
		services.AddTransientWithShellRoute<UniformItemsLayoutPage, UniformItemsLayoutViewModel>();

		// Add PlatformSpecific Pages + ViewModels
		services.AddTransientWithShellRoute<NavigationBarPage, NavigationBarAndroidViewModel>();

		// Add Views Pages + ViewModels
		services.AddTransientWithShellRoute<BasicMapsPage, BasicMapsViewModel>();
		services.AddTransientWithShellRoute<CameraViewPage, CameraViewViewModel>();
		services.AddTransientWithShellRoute<CustomSizeAndPositionPopupPage, CustomSizeAndPositionPopupViewModel>();
		services.AddTransientWithShellRoute<DrawingViewPage, DrawingViewViewModel>();
		services.AddTransientWithShellRoute<ExpanderPage, ExpanderViewModel>();
		services.AddTransientWithShellRoute<LazyViewPage, LazyViewViewModel>();
		services.AddTransientWithShellRoute<MapsPinsPage, MapsPinsViewModel>();
		services.AddTransientWithShellRoute<MediaElementPage, MediaElementViewModel>();
		services.AddTransientWithShellRoute<MediaElementCarouselViewPage, MediaElementCarouselViewViewModel>();
		services.AddTransientWithShellRoute<MediaElementCollectionViewPage, MediaElementCollectionViewViewModel>();
		services.AddTransientWithShellRoute<MediaElementMultipleWindowsPage, MediaElementMultipleWindowsViewModel>();
		services.AddTransientWithShellRoute<MultiplePopupPage, MultiplePopupViewModel>();
		services.AddTransientWithShellRoute<PopupAnchorPage, PopupAnchorViewModel>();
		services.AddTransientWithShellRoute<PopupLayoutAlignmentPage, PopupLayoutAlignmentViewModel>();
		services.AddTransientWithShellRoute<PopupPositionPage, PopupPositionViewModel>();
		services.AddTransientWithShellRoute<RatingViewCsharpPage, RatingViewCsharpViewModel>();
		services.AddTransientWithShellRoute<RatingViewShowcasePage, RatingViewShowcaseViewModel>();
		services.AddTransientWithShellRoute<RatingViewXamlPage, RatingViewXamlViewModel>();
		services.AddTransientWithShellRoute<SemanticOrderViewPage, SemanticOrderViewPageViewModel>();
		services.AddTransientWithShellRoute<ShowPopupInOnAppearingPage, ShowPopupInOnAppearingPageViewModel>();
		services.AddTransientWithShellRoute<StylePopupPage, StylePopupViewModel>();
		services.AddTransientWithShellRoute<PopupSizingIssuesPage, PopupSizingIssuesViewModel>();

		// Add Popups
		services.AddTransientPopup<CsharpBindingPopup, CsharpBindingPopupViewModel>();
		services.AddTransientPopup<UpdatingPopup, UpdatingPopupViewModel>();
		services.AddTransientPopup<XamlBindingPopup, XamlBindingPopupViewModel>();
	}

	static void RegisterEssentials(in IServiceCollection services)
	{
		services.AddSingleton<IDeviceDisplay>(DeviceDisplay.Current);
		services.AddSingleton<IDeviceInfo>(DeviceInfo.Current);
		services.AddSingleton<IFileSaver>(FileSaver.Default);
		services.AddSingleton<IFileSystem>(FileSystem.Current);
		services.AddSingleton<IFolderPicker>(FolderPicker.Default);
		services.AddSingleton<IBadge>(Badge.Default);
		services.AddKeyedSingleton<ISpeechToText, SpeechToTextImplementation>("Online");
		services.AddKeyedSingleton<ISpeechToText, OfflineSpeechToTextImplementation>("Offline");
		services.AddSingleton<ITextToSpeech>(TextToSpeech.Default);
	}

	static IServiceCollection AddTransientWithShellRoute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPage, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(this IServiceCollection services) where TPage : BasePage<TViewModel>
																												where TViewModel : BaseViewModel
	{
		return services.AddTransientWithShellRoute<TPage, TViewModel>(AppShell.GetPageRoute<TViewModel>());
	}

	sealed class MobileHttpRetryStrategyOptions : HttpRetryStrategyOptions
	{
		public MobileHttpRetryStrategyOptions()
		{
			BackoffType = DelayBackoffType.Exponential;
			MaxRetryAttempts = 3;
			UseJitter = true;
			Delay = TimeSpan.FromSeconds(2);
		}
	}
}