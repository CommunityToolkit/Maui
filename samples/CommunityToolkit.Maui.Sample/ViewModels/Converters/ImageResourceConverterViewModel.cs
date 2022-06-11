namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ImageResourceConverterViewModel : BaseViewModel
{
	public string XamarinImageResource { get; } = BuildImageResource("XCT.png");
	public string MauiImageResource { get; } = BuildImageResource("MCT.png");

	static string BuildImageResource(in string resourceName) => $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.Resources.Embedded.{resourceName}";
}