namespace CommunityToolkit.Maui.Converters
{
	/// <summary>
	/// The text cases that can be used with <see cref="TextCaseConverter"/> to convert text to a specific case.
	/// </summary>
	public enum TextCaseType
	{
		/// <summary>Should not be converted</summary>
		None,

		/// <summary>Convert to uppercase</summary>
		Upper,

		/// <summary>Convert to lowercase</summary>
		Lower,

		/// <summary>Converts the first letter to upper only</summary>
		FirstUpperRestLower,
	}
}