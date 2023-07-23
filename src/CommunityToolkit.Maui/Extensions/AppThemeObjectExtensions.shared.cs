namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// This class contains static extension methods for use with <see cref="BindableObject"/> objects.
/// </summary>
public static class AppThemeObjectExtensions
{
	/// <summary>
	/// Sets the <see cref="AppThemeColor"/> to the provided <see cref="BindableProperty"/> of the given <see cref="BindableObject"/>.
	/// </summary>
	/// <param name="self">The <see cref="BindableObject"/> on which the <paramref name="appThemeColor"/> will be applied to the provided property in <paramref name="targetProperty"/>.</param>
	/// <param name="targetProperty">The <see cref="BindableProperty"/> on which to set the <paramref name="appThemeColor"/>.</param>
	/// <param name="appThemeColor">The <see cref="AppThemeColor"/> to apply to <paramref name="targetProperty"/>.</param>
	public static void SetAppThemeColor(this BindableObject self, BindableProperty targetProperty, AppThemeColor appThemeColor) =>
		self.SetBinding(targetProperty, appThemeColor.GetBinding());

	/// <summary>
	/// Sets the <see cref="AppThemeObject"/> to the provided <see cref="BindableProperty"/> of the given <see cref="BindableObject"/>.
	/// </summary>
	/// <param name="self">The <see cref="BindableObject"/> on which the <paramref name="appThemeResource"/> will be applied to the provided property in <paramref name="targetProperty"/>.</param>
	/// <param name="targetProperty">The <see cref="BindableProperty"/> on which to set the <paramref name="appThemeResource"/>.</param>
	/// <param name="appThemeResource">The <see cref="AppThemeObject"/> to apply to <paramref name="targetProperty"/>.</param>
	public static void SetAppTheme<T>(this BindableObject self, BindableProperty targetProperty, AppThemeObject<T> appThemeResource) =>
		self.SetBinding(targetProperty, appThemeResource.GetBinding());
}