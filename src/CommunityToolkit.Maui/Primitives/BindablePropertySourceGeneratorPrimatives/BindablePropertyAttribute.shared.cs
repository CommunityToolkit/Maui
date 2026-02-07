namespace CommunityToolkit.Maui;

/// <summary>
/// Generates a <see cref="Microsoft.Maui.Controls.BindableProperty"/> when placed on a <see langword="partial"/> property.
/// </summary>
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.AttributeUsage(global::System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
[global::System.Diagnostics.CodeAnalysis.Experimental(BindablePropertyDiagnostic.BindablePropertyAttributeExperimentalDiagnosticId)]
public sealed partial class BindablePropertyAttribute : global::System.Attribute
{
	/// <summary>
	/// The default <see cref="Microsoft.Maui.Controls.BindingMode"/> for the generated <see cref="Microsoft.Maui.Controls.BindableProperty"/>. The default is <see cref="Microsoft.Maui.Controls.BindingMode.Default"/>.
	/// </summary>
	public global::Microsoft.Maui.Controls.BindingMode DefaultBindingMode { get; init; }

	/// <summary>
	/// The name of the method to call to validate the value of the <see cref="BindableProperty"/>. 
	/// </summary>
	/// <remarks>
	/// The method must be <see langword="static"/> and have the following signature: <c>static bool [MethodName](object value)</c>.
	/// </remarks>
	public string? ValidateValueMethodName { get; init; }

	/// <summary>
	/// Gets the name of the method to invoke when the <see cref="BindableProperty"/> value changes.
	/// </summary>
	/// <remarks>
	/// The method must be <see langword="static"/> and have the following signature: <c>static void [MethodName](BindableObject binable, object oldValue, object newValue)</c>.
	/// </remarks>
	public string? PropertyChangedMethodName { get; init; }

	/// <summary>
	/// Gets the name of the method that is invoked when the <see cref="BindableProperty"/> is about to change.
	/// </summary>
	/// <remarks>
	/// The method must be <see langword="static"/> and have the following signature: <c>static void [MethodName](BindableObject binable, object oldValue, object newValue)</c>.
	/// </remarks>
	public string? PropertyChangingMethodName { get; init; }

	/// <summary>
	/// Gets the name of the method used to coerce the value of the <see cref="BindableProperty"/>.
	/// </summary>
	/// <remarks>
	/// The method must be <see langword="static"/> and have the following signature: <c>static object [MethodName](BindableObject binable, object value)</c>.
	/// </remarks>
	public string? CoerceValueMethodName { get; init; }

	/// <summary>
	/// Gets the name of the method used to create the default value for the <see cref="BindableProperty"/>.
	/// </summary>
	/// <remarks>
	/// The method must be <see langword="static"/> and have the following signature: <c>static object [MethodName]()</c>.
	/// </remarks>
	public string? DefaultValueCreatorMethodName { get; init; }
}
