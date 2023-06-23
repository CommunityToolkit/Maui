namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// 
/// </summary>
public static class BindableObjectExtensions
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="self"></param>
	/// <param name="targetProperty"></param>
	/// <param name="appThemeColor"></param>
	public static void SetAppThemeColor(this BindableObject self, BindableProperty targetProperty, AppThemeColor appThemeColor) =>
		self.SetBinding(targetProperty, appThemeColor.GetBinding());

	/// <summary>
	/// 
	/// </summary>
	/// <param name="self"></param>
	/// <param name="targetProperty"></param>
	/// <param name="resource"></param>
	public static void SetAppTheme(this BindableObject self, BindableProperty targetProperty, AppThemeResource resource) =>
		self.SetBinding(targetProperty, resource.GetBinding());
}

