namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="UriValidationBehavior"/> is a behavior that allows users to determine whether or not text input is a valid URI. For example, an <see cref="Entry"/> control can be styled differently depending on whether a valid or an invalid URI is provided. Additional properties handling validation are inherited from <see cref="ValidationBehavior"/>.
/// </summary>
public class UriValidationBehavior : TextValidationBehavior
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="UriKind"/> property.
	/// </summary>
	public static readonly BindableProperty UriKindProperty =
		BindableProperty.Create(nameof(UriKind), typeof(UriKind), typeof(UriValidationBehavior), UriKind.RelativeOrAbsolute, propertyChanged: OnValidationPropertyChanged);

	/// <summary>
	/// Provides an enumerated value that specifies how to handle different URI types. This is a bindable property.
	/// </summary>
	public UriKind UriKind
	{
		get => (UriKind)GetValue(UriKindProperty);
		set => SetValue(UriKindProperty, value);
	}

	/// <inheritdoc/>
	protected override async ValueTask<bool> ValidateAsync(string? value, CancellationToken token)
		=> await base.ValidateAsync(value, token).ConfigureAwait(false) && Uri.IsWellFormedUriString(value, UriKind);
}