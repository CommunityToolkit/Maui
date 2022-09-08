using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Converters;

namespace CommunityToolkit.Maui;

/// <summary>
/// .NET MAUI Community Toolkit Options.
/// </summary>
public class Options : Core.Options
{
	internal static bool ThrowExceptionInAnimations { get; private set; } = true;
	internal static bool ThrowExceptionInConverters { get; private set; } = true;
	internal static bool ThrowExceptionInBehaviors { get; private set; } = true;

	/// <summary>
	/// Allows to return default value instead of throwing an exception when using <see cref="BaseConverter{TFrom,TTo}"/>.
	/// Default value is true.
	/// </summary>
	public void SetThrowExceptionInConverters(bool value) => ThrowExceptionInConverters = value;	

	/// <summary>
	/// Allows to return default value instead of throwing an exception when using <see cref="AnimationBehavior"/>.
	/// Default value is true.
	/// </summary>
	public void SetThrowExceptionInAnimations(bool value) => ThrowExceptionInAnimations = value;	
	
	/// <summary>
	/// Allows to return default value instead of throwing an exception when using <see cref="BaseBehavior{TView}"/>.
	/// Default value is true.
	/// </summary>
	public void SetThrowExceptionInBehaviors(bool value) => ThrowExceptionInBehaviors = value;
}