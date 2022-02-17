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

		// Add Gallery View Models
		builder.Services.AddTransient<AlertsGalleryViewModel>();
		builder.Services.AddTransient<BehaviorsGalleryViewModel>();
		builder.Services.AddTransient<ConvertersGalleryViewModel>();
		builder.Services.AddTransient<ExtensionsGalleryViewModel>();
		builder.Services.AddTransient<LayoutsGalleryViewModel>();
		builder.Services.AddTransient<MainGalleryViewModel>();
		builder.Services.AddTransient<ViewsGalleryViewModel>();

		// Add Behaviors View Models
		builder.Services.AddTransient<EventToCommandBehaviorViewModel>();
		builder.Services.AddTransient<MaxLengthReachedBehaviorViewModel>();
		builder.Services.AddTransient<ProgressBarAnimationBehaviorViewModel>();
		builder.Services.AddTransient<UserStoppedTypingBehaviorViewModel>();

		// Add Converters View Models
		builder.Services.AddTransient<CompareConverterViewModel>();
		builder.Services.AddTransient<DateTimeOffsetConverterViewModel>();
		builder.Services.AddTransient<DoubleToIntConverterViewModel>();
		builder.Services.AddTransient<EnumToIntConverterViewModel>();
		builder.Services.AddTransient<EqualConverterViewModel>();
		builder.Services.AddTransient<IndexToArrayItemConverterViewModel>();
		builder.Services.AddTransient<IntToBoolConverterViewModel>();
		builder.Services.AddTransient<IsStringNotNullOrEmptyConverterViewModel>();
		builder.Services.AddTransient<IsStringNotNullOrWhiteSpaceConverterViewModel>();
		builder.Services.AddTransient<IsStringNullOrEmptyConverterViewModel>();
		builder.Services.AddTransient<IsStringNullOrWhiteSpaceConverterViewModel>();
		builder.Services.AddTransient<ItemSelectedEventArgsConverterViewModel>();
		builder.Services.AddTransient<ItemTappedEventArgsConverterViewModel>();
		builder.Services.AddTransient<IsListNotNullOrEmptyConverterViewModel>();
		builder.Services.AddTransient<IsListNullOrEmptyConverterViewModel>();
		builder.Services.AddTransient<ListToStringConverterViewModel>();

		// Add Extensions
		builder.Services.AddTransient<ColorAnimationExtensionsViewModel>();


		// Add Popup View Models
		builder.Services.AddTransient<CsharpBindingPopupViewModel>();
		builder.Services.AddTransient<PopupAnchorViewModel>();
		builder.Services.AddTransient<PopupPositionViewModel>();
		builder.Services.AddTransient<XamlBindingPopupViewModel>();

		// Add Layouts View Models
		builder.Services.AddTransient<UniformItemsLayoutViewModel>();

		// Add Views View Models
		builder.Services.AddTransient<PopupAnchorViewModel>();
		builder.Services.AddTransient<PopupPositionViewModel>();

		return builder.Build();
	}
}
