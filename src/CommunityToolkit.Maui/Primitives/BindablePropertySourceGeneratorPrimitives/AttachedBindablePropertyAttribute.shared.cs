namespace CommunityToolkit.Maui;

/// <summary> Source generator that creates an Attached <see cref="Microsoft.Maui.Controls.BindableProperty"/> and two <see langword="static"/> methods: <c>Get{PropertyName}(BindableObject bindable)</c> and <c>Set{PropertyName}(BindableObject bindable, T value)</c></summary>
/// <typeparam name="T">Type of the Attached Bindable Property. Set <see cref="AttachedBindablePropertyAttribute{T}.IsNullable"/> to <see langword="true"/> to generate a nullable type for <typeparamref name="T"/></typeparam>
/// <param name="propertyName">Name of the Attached Property</param>
/// <remarks> 
///* Generates a <see langword="readonly"/> <see langword="static"/> <see cref="Microsoft.Maui.Controls.BindableProperty"/> field using <see cref="Microsoft.Maui.Controls.BindableProperty.CreateAttached(string, Type, Type, object, BindingMode, BindableProperty.ValidateValueDelegate, BindableProperty.BindingPropertyChangedDelegate, BindableProperty.BindingPropertyChangingDelegate, BindableProperty.CoerceValueDelegate, BindableProperty.BindablePropertyBindingChanging, bool, BindableProperty.CreateDefaultValueDelegate)"/><br/>
///* Generates <see langword="static"/> <c>Get{PropertyName}(BindableObject bindable)</c><br/>
///* Generates <see langword="static"/> <c>Set{PropertyName}(BindableObject bindable, T value)</c><br/>
///* The property type <typeparamref name="T"/> will be treated as non-nullable unless <see cref="AttachedBindablePropertyAttribute{T}.IsNullable"/> is set to <see langword="true"/>
/// </remarks>
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Constructor, AllowMultiple = true, Inherited = false)]
[global::System.Diagnostics.CodeAnalysis.Experimental(BindablePropertyDiagnostic.BindablePropertyAttributeExperimentalDiagnosticId)]
public sealed partial class AttachedBindablePropertyAttribute<T>(string propertyName) : global::System.Attribute where T : notnull
{
	/// <summary>
	/// Name of the Attached Property
	/// </summary>
	public string PropertyName { get; } = propertyName;

	/// <summary>
	/// Should generate a nullable type for T
	/// </summary>
	public bool IsNullable { get; init; }

	/// <summary>
	/// The default value for the property
	/// </summary>
	public T? DefaultValue { get; init; }

	/// <summary>
	/// The BindingMode to use on SetBinding() if no BindingMode is given. Default is <see cref="Microsoft.Maui.Controls.BindingMode.Default"/>
	/// </summary>
	public global::Microsoft.Maui.Controls.BindingMode DefaultBindingMode { get; init; } = global::Microsoft.Maui.Controls.BindingMode.Default;

	/// <summary>
	/// The name of the method to call to validate the value of the <see cref="BindableProperty"/>. 
	/// </summary>
	/// <remarks>
	/// The method must be <see langword="static"/> and have the following signature: <c>static bool {MethodName}(BindableObject bindable, object value)</c>.
	/// </remarks>
	public string? ValidateValueMethodName { get; init; }

	/// <summary>
	/// Gets the name of the method to invoke when the <see cref="BindableProperty"/> value changes.
	/// </summary>
	/// <remarks>
	/// The method must be <see langword="static"/> and have the following signature: <c>static void {MethodName}(BindableObject bindable, object oldValue, object newValue)</c>.
	/// </remarks>
	public string? PropertyChangedMethodName { get; init; }

	/// <summary>
	/// Gets the name of the method that is invoked when the <see cref="BindableProperty"/> is about to change.
	/// </summary>
	/// <remarks>
	/// The method must be <see langword="static"/> and have the following signature: <c>static void {MethodName}(BindableObject bindable, object oldValue, object newValue)</c>.
	/// </remarks>
	public string? PropertyChangingMethodName { get; init; }

	/// <summary>
	/// Gets the name of the method used to coerce the value of the <see cref="BindableProperty"/>.
	/// </summary>
	/// <remarks>
	/// The method must be <see langword="static"/> and have the following signature: <c>static object {MethodName}(BindableObject bindable, object value)</c>.
	/// </remarks>
	public string? CoerceValueMethodName { get; init; }

	/// <summary>
	/// Gets the name of the method used to create the default value for the <see cref="BindableProperty"/>.
	/// </summary>
	/// <remarks>
	/// The method must be <see langword="static"/> and have the following signature: <c>static object {MethodName}()</c>.
	/// </remarks>
	public string? DefaultValueCreatorMethodName { get; init; }

	/// <summary>Custom XML Comments added to BindableProperty</summary>
	/// <remarks>
	/// Must be the entire XML string, including <c>/// </c>
	/// </remarks>
	public string? BindablePropertyXmlDocumentation { get; init; }

	/// <summary>
	/// Custom XML Comments added to <c>Get{PropertyName}(BindableObject bindable)</c>
	/// </summary>
	/// <remarks>
	/// Must be the entire XML string, including <c>/// </c>
	/// </remarks>
	public string? GetterMethodXmlDocumentation { get; init; }

	/// <summary>
	/// Custom XML Comments added to <c>Set{PropertyName}(BindableObject bindable, T value)</c>
	/// </summary>
	/// <remarks>
	/// Must be the entire XML string, including <c>/// </c>
	/// </remarks>
	public string? SetterMethodXmlDocumentation { get; init; }

	/// <summary>
	/// The access modifier applied to the generated field <see cref="Microsoft.Maui.Controls.BindableProperty"/>. The default is <see cref="CommunityToolkit.Maui.AccessModifier.Public"/>.
	/// </summary>
	public global::CommunityToolkit.Maui.AccessModifier BindablePropertyAccessibility { get; init; } = global::CommunityToolkit.Maui.AccessModifier.Public;

	/// <summary>
	/// The access modifier applied to the generated method <c>Get{PropertyName}(BindableObject bindable)</c>. The default is <see cref="CommunityToolkit.Maui.AccessModifier.Public"/>.
	/// </summary>
	public global::CommunityToolkit.Maui.AccessModifier GetterAccessibility { get; init; } = global::CommunityToolkit.Maui.AccessModifier.Public;

	/// <summary>
	/// The access modifier applied to the generated method <c>Set{PropertyName}(BindableObject bindable, T value)</c>. The default is <see cref="CommunityToolkit.Maui.AccessModifier.Public"/>.
	/// </summary>
	public global::CommunityToolkit.Maui.AccessModifier SetterAccessibility { get; init; } = global::CommunityToolkit.Maui.AccessModifier.Public;
}