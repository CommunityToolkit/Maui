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

		// Add Gallery View Models
		builder.Services.AddTransient<AlertsGalleryViewModel>();
		builder.Services.AddTransient<BehaviorsGalleryViewModel>();
		builder.Services.AddTransient<ConvertersGalleryViewModel>();
		builder.Services.AddTransient<ExtensionsGalleryViewModel>();
		builder.Services.AddTransient<MainGalleryViewModel>();

		// Add Behaviors View Models
		builder.Services.AddTransient<EventToCommandBehaviorViewModel>();
		builder.Services.AddTransient<MaxLengthReachedBehaviorViewModel>();
		builder.Services.AddTransient<ProgressBarAnimationBehaviorViewModel>();
		builder.Services.AddTransient<UserStoppedTypingBehaviorViewModel>();
		
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

		// Add Extensions
		builder.Services.AddTransient<ColorAnimationExtensionsViewModel>();

		return builder.Build();
	}
}