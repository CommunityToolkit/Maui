using System;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// Validation flags
/// </summary>
[Flags]
public enum ValidationFlags
{
	/// <summary>
	/// No validation
	/// </summary>
	None = 0,
	/// <summary>
	/// Validate on attaching
	/// </summary>
	ValidateOnAttaching = 1,
	/// <summary>
	/// Validate on focusing
	/// </summary>
	ValidateOnFocusing = 2,
	/// <summary>
	/// Validate on unfocusing
	/// </summary>
	ValidateOnUnfocusing = 4,
	/// <summary>
	/// Validate upon value changed
	/// </summary>
	ValidateOnValueChanged = 8,
	/// <summary>
	/// Force make valid when focused
	/// </summary>
	ForceMakeValidWhenFocused = 16
}