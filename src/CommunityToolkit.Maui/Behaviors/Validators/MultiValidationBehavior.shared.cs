using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="MultiValidationBehavior"/> is a behavior that allows the user to combine multiple validators to validate text input depending on specified parameters. For example, an <see cref="Entry"/> control can be styled differently depending on whether a valid or an invalid text input is provided. By allowing the user to chain multiple existing validators together, it offers a high degree of customizability when it comes to validation. Additional properties handling validation are inherited from <see cref="ValidationBehavior"/>.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
[RequiresUnreferencedCode($"{nameof(MultiValidationBehavior)} is not trim safe because it uses bindings with string paths.")]
[ContentProperty(nameof(Children))]
[AttachedBindableProperty<object>("Error", IsNullable = true, GetterMethodXmlDocumentation = getErrorMethodXmlDocumentation, SetterMethodXmlDocumentation = setErrorMethodXmlDocumentation)]
public partial class MultiValidationBehavior : ValidationBehavior
{
	const string getErrorMethodXmlDocumentation =
		/* language=C#-test */
		//lang=csharp
		"""
		/// <summary>
		/// Method to extract the error from the attached property for a child behavior in <see cref="Children"/>.
		/// </summary>
		/// <param name="bindable">The <see cref="ValidationBehavior"/> that we extract the attached Error property</param>
		/// <returns>Object containing error information</returns>
		""";

	const string setErrorMethodXmlDocumentation =
		/* language=C#-test */
		//lang=csharp
		"""
		/// <summary>
		/// Method to set the error on the attached property for a child behavior in <see cref="Children"/>.
		/// </summary>
		/// <param name="bindable">The <see cref="ValidationBehavior"/> on which we set the attached Error property value</param>
		/// <param name="value">The value to set</param>
		""";

	readonly ObservableCollection<ValidationBehavior> children = [];

	/// <summary>
	/// All child behaviors that are part of this <see cref="MultiValidationBehavior"/>. This is a bindable property.
	/// </summary>
	public IList<ValidationBehavior> Children => children;

	/// <summary>
	/// Holds the errors from all the nested invalid validators in <see cref="Children"/>. This is a bindable property.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial List<object?>? Errors { get; set; } = MultiValidationBehaviorDefaults.Errors;

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