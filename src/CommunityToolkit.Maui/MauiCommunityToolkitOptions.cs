using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Converters;

namespace CommunityToolkit.Maui;

/// <summary>
/// .NET MAUI Community Toolkit Options.
/// </summary>
public class MauiCommunityToolkitOptions
{
#pragma warning disable CA1822 // Mark members as static
	/// <summary>
	/// Allows to return default value instead of throwing an exception when using <see cref="BaseConverter{TFrom,TTo}"/>.
	/// Default value is true.
	/// </summary>
	public void SetThrowExceptionInConverters(bool value) => ThrowExceptionInConverters = value;
	
	internal static bool ThrowExceptionInConverters { get; private set; } = true;

	/// <summary>
	/// Allows to return default value instead of throwing an exception when using <see cref="AnimationBehavior"/>.
	/// Default value is true.
	/// </summary>
	public void SetThrowExceptionInAnimations(bool value) => ThrowExceptionInAnimations = value;
	
	internal static bool ThrowExceptionInAnimations { get; private set; } = true;
#pragma warning restore CA1822 // Mark members as static
}