namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ImageResourceConverterViewModel : BaseViewModel
{
	public string XamarinImageResource => BuildImageResource("XCT.png");
	public string MauiImageResource => BuildImageResource("MCT.png");

	static string BuildImageResource(string resourceName) => $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.Resources.Embedded.{resourceName}";
}