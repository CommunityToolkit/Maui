namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="RequiredStringValidationBehavior"/> is a behavior that allows the user to determine if text input is equal to specific text. For example, an <see cref="Entry"/> control can be styled differently depending on whether a valid or an invalid text input is provided. Additional properties handling validation are inherited from <see cref="ValidationBehavior"/>.
/// </summary>
public class RequiredStringValidationBehavior : ValidationBehavior<string>
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="RequiredString"/> property.
	/// </summary>
	public static readonly BindableProperty RequiredStringProperty
		= BindableProperty.Create(nameof(RequiredString), typeof(string), typeof(RequiredStringValidationBehavior));

	/// <summary>
	/// Backing BindableProperty for the <see cref="ExactMatch"/> property.
	/// </summary>
	public static readonly BindableProperty ExactMatchProperty
		= BindableProperty.Create(nameof(ExactMatch), typeof(bool), typeof(RequiredStringValidationBehavior), true);

	/// <summary>
	/// The string that will be compared to the value provided by the user. This is a bindable property.
	/// </summary>
	public string? RequiredString
	{
		get => (string?)GetValue(RequiredStringProperty);
		set => SetValue(RequiredStringProperty, value);
	}

	/// <summary>
	/// Get or sets whether the entered text must match the whole contents of the <see cref="RequiredString"/> property
	/// or simply contain the <see cref="RequiredString"/> property value.
	/// <br/>
	/// <c>true</c> by default. This is a bindable property.
	/// </summary>
	public bool ExactMatch
	{
		get => (bool)GetValue(ExactMatchProperty);
		set => SetValue(ExactMatchProperty, value);
	}

	/// <inheritdoc/>
	protected override ValueTask<bool> ValidateAsync(string? value, CancellationToken token)
	{
#pragma warning disable CA1309 // Use ordinal string comparison - It is an entirely valid use case to use the current culture when validating what the user has entered.
		return new ValueTask<bool>(ExactMatch switch
		{
			true => string.Equals(value, RequiredString, StringComparison.CurrentCulture),
			false => value?.Contains(RequiredString ?? string.Empty, StringComparison.CurrentCulture) ?? false
		});
#pragma warning restore CA1309 // Use ordinal string comparison
	}
}