using System;

namespace CommunityToolkit.Maui.Behaviors
{
	/// <summary>
	/// Flags to indicate what treatment <see cref="Internals.ValidationBehavior.Value"/> should receive prior to validation with <see cref="Internals.ValidationBehavior"/> or subclasses. This can be used to trim or ignore whitespace for instance. This value might be ignored by a behavior if <see cref="Internals.ValidationBehavior.Value"/> isn't of type <see cref="string"/>.
	/// </summary>
	[Flags]
	public enum TextDecorationFlags
	{
		/// <summary>No text decoration will be applied.</summary>
		None = 0,

		/// <summary><see cref="string.TrimStart"/> is applied on the value prior to validation.</summary>
		TrimStart = 1,

		/// <summary><see cref="string.TrimEnd"/> is applied on the value prior to validation.</summary>
		TrimEnd = 2,

		/// <summary><see cref="string.Trim"/> is applied on the value prior to validation.</summary>
		Trim = 3,

		/// <summary>If <see cref="Internals.ValidationBehavior.Value"/> is null, replace the value with <see cref="string.Empty"/></summary>
		NullToEmpty = 4,

		/// <summary>Excessive white space is removed from <see cref="Internals.ValidationBehavior.Value"/> prior to validation. I.e. I.e. "Hello    World" will become "Hello World". This applies to whitespace found anywhere.</summary>
		NormalizeWhiteSpace = 8
	}
}