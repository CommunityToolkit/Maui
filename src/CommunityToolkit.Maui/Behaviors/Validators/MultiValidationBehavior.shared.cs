using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="MultiValidationBehavior"/> is a behavior that allows the user to combine multiple validators to validate text input depending on specified parameters. For example, an <see cref="Entry"/> control can be styled differently depending on whether a valid or an invalid text input is provided. By allowing the user to chain multiple existing validators together, it offers a high degree of customizability when it comes to validation. Additional properties handling validation are inherited from <see cref="ValidationBehavior"/>.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
[RequiresUnreferencedCode($"{nameof(MultiValidationBehavior)} is not trim safe because it uses bindings with string paths.")]
[ContentProperty(nameof(Children))]
public partial class MultiValidationBehavior : ValidationBehavior
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="Errors"/> property.
	/// </summary>
	public static readonly BindableProperty ErrorsProperty =
		BindableProperty.Create(nameof(Errors), typeof(List<object?>), typeof(MultiValidationBehavior), null, BindingMode.OneWayToSource);

	/// <summary>
	/// BindableProperty for getting the error.
	/// </summary>
	public static readonly BindableProperty ErrorProperty =
		BindableProperty.CreateAttached(nameof(GetError), typeof(object), typeof(MultiValidationBehavior), null);

	readonly ObservableCollection<ValidationBehavior> children = [];

	/// <summary>
	/// All child behaviors that are part of this <see cref="MultiValidationBehavior"/>. This is a bindable property.
	/// </summary>
	public IList<ValidationBehavior> Children => children;

	/// <summary>
	/// Holds the errors from all the nested invalid validators in <see cref="Children"/>. This is a bindable property.
	/// </summary>
	public List<object?>? Errors
	{
		get => (List<object?>?)GetValue(ErrorsProperty);
		set => SetValue(ErrorsProperty, value);
	}

	/// <summary>
	/// Method to extract the error from the attached property for a child behavior in <see cref="Children"/>.
	/// </summary>
	/// <param name="bindable">The <see cref="ValidationBehavior"/> that we extract the attached Error property</param>
	/// <returns>Object containing error information</returns>
	public static object? GetError(BindableObject bindable) => bindable.GetValue(ErrorProperty);

	/// <summary>
	/// Method to set the error on the attached property for a child behavior in <see cref="Children"/>.
	/// </summary>
	/// <param name="bindable">The <see cref="ValidationBehavior"/> on which we set the attached Error property value</param>
	/// <param name="value">The value to set</param>
	public static void SetError(BindableObject bindable, object value) => bindable.SetValue(ErrorProperty, value);

	/// <inheritdoc/>
	protected override async ValueTask<bool> ValidateAsync(object? value, CancellationToken token)
	{
		await Task.WhenAll(children.Select(async validationBehavior =>
		{
			validationBehavior.Value = value;
			await validationBehavior.ValidateNestedAsync(token);
		})).ConfigureAwait(false);

		var errors = children.Where(static c => c.IsNotValid).Select(GetError).ToList();

		if (errors.Count is 0)
		{
			Errors = null;
			return true;
		}

		if (!Errors?.SequenceEqual(errors) ?? true)
		{
			Errors = errors;
		}

		return false;
	}
}