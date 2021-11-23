using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="EmailValidationBehavior"/> is a behavior that allows users to determine whether or not text input is a valid e-mail address. 
/// For example, an <see cref="Entry"/> control can be styled differently depending on whether a valid or an invalid e-mail address is provided.
/// The validation is achieved through a regular expression that is used to verify whether or not the text input is a valid e-mail address.
/// It can be overridden to customize the validation through the properties it inherits from <see cref="Internals.ValidationBehavior"/>.
/// </summary>
public class EmailValidationBehavior : TextValidationBehavior
{
	/// <inheritdoc /> 
	protected override async ValueTask<bool> ValidateAsync(object? value, CancellationToken token)
	{
		var text = (string?)value;
		return MailAddress.TryCreate(text ?? string.Empty, out _) && await base.ValidateAsync(value, token);
	}
}