using System.Globalization;
using System.Text.RegularExpressions;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="EmailValidationBehavior"/> is a behavior that allows users to determine whether or not text input is a valid e-mail address. 
/// For example, an <see cref="Entry"/> control can be styled differently depending on whether a valid or an invalid e-mail address is provided.
/// The validation is achieved through a regular expression that is used to verify whether or not the text input is a valid e-mail address.
/// It can be overridden to customize the validation through the properties it inherits from <see cref="ValidationBehavior"/>.
/// </summary>
public partial class EmailValidationBehavior : TextValidationBehavior
{
	/// <inheritdoc /> 
	protected override async ValueTask<bool> ValidateAsync(string? value, CancellationToken token)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		if (value.StartsWith('.'))
		{
			return false;
		}

		if (value.Contains("..", StringComparison.Ordinal))
		{
			return false;
		}

		if (value.Contains(".@", StringComparison.Ordinal))
		{
			return false;
		}

		return IsValidEmail(value) && await base.ValidateAsync(value, token);
	}

	/// <inheritdoc /> 
	protected override void OnAttachedTo(VisualElement bindable)
	{
		// Assign Keyboard.Email if the user has not specified a specific Keyboard layout
		if (bindable is InputView inputView && inputView.Keyboard == Keyboard.Default)
		{
			inputView.Keyboard = Keyboard.Email;
		}

		base.OnAttachedTo(bindable);
	}

	/// <inheritdoc /> 
	protected override void OnDetachingFrom(VisualElement bindable)
	{
		// Assign Keyboard.Default if the user has not specified a different Keyboard layout
		if (bindable is InputView inputView && inputView.Keyboard == Keyboard.Email)
		{
			inputView.Keyboard = Keyboard.Default;
		}

		base.OnDetachingFrom(bindable);
	}

	// https://docs.microsoft.com/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format
	static bool IsValidEmail(string? email)
	{
		if (string.IsNullOrWhiteSpace(email))
		{
			return false;
		}

		try
		{
			// Normalize the domain
			email = NormalizeDomainRegex().Replace(email, DomainMapper);
		}
		catch (RegexMatchTimeoutException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}

		try
		{
			return EmailRegex().IsMatch(email);
		}
		catch (RegexMatchTimeoutException)
		{
			return false;
		}

		// Examines the domain part of the email and normalizes it.
		static string DomainMapper(Match match)
		{
			// Use IdnMapping class to convert Unicode domain names.
			var idn = new IdnMapping();

			// Pull out and process domain name (throws ArgumentException on invalid)
			string domainName = idn.GetAscii(match.Groups[2].Value);

			if (domainName.All(x => char.IsDigit(x) || x is '.')
				&& !ValidIpv4Regex().IsMatch(domainName))
			{
				throw new ArgumentException("Invalid IPv4 Address.");
			}

			if (domainName.StartsWith('-'))
			{
				throw new ArgumentException("Domain name cannot start with hyphen.");
			}

			return match.Groups[1].Value + domainName;
		}
	}

	[GeneratedRegex(@"^([1-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])(\.([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])){3}$", RegexOptions.None, 250)]
	private static partial Regex ValidIpv4Regex();

	[GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, 250)]
	private static partial Regex EmailRegex();

	[GeneratedRegex(@"(@)(.+)$", RegexOptions.None, 250)]
	private static partial Regex NormalizeDomainRegex();
}