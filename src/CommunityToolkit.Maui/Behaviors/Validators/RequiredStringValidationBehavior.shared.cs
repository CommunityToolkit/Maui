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
	/// Equals comapre or contains. Equals by default. This is a bindable property.
	/// </summary>
	public bool ExactMatch
	{
		get => (bool)GetValue(ExactMatchProperty);
		set => SetValue(ExactMatchProperty, value);
	}

	/// <inheritdoc/>
	protected override ValueTask<bool> ValidateAsync(string? value, CancellationToken token)
	{
		return new ValueTask<bool>(ExactMatch switch
		{
			true => value?.ToString() == RequiredString,
			false => value?.ToString()?.Contains(RequiredString ?? string.Empty) ?? false
		});
	}
}