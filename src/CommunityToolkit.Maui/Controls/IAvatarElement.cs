using System.ComponentModel;

namespace CommunityToolkit.Maui.Controls;

/// <summary>Avatar content view element interface.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IAvatarElement
{
	#region Public Property

	//// NOTE to implementer: Implement these properties as public.

	/// <summary>Gets a value indicating the Avatar text.</summary>
	string Text { get; }

	/// <summary>Gets a value indicating the avatar corner radius (MAUI).</summary>
	CornerRadius CornerRadius { get; }

	#endregion Public Property

	#region Explicit methods

	//// NOTE to implementer: Implement these methods explicitly

	/// <summary>Gets a value indicating the text default.</summary>
	string TextDefaultValue { get; }

	/// <summary>Gets a value indicating the corner radius default.</summary>
	CornerRadius CornerRadiusDefaultValue { get; }

	/// <summary>Indicates whether avatar text is set.</summary>
	/// <returns>True if set.</returns>
	bool IsTextSet();

	/// <summary>On text property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnTextPropertyChanged(string oldValue, string newValue);

	/// <summary>On corner radius property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnCornerRadiusPropertyChanged(CornerRadius oldValue, CornerRadius newValue);

	/// <summary>On image source property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnImageSourceChanged(ImageSource oldValue, ImageSource newValue);

	#endregion Explicit methods
}