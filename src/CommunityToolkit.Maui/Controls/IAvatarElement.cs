using System.ComponentModel;

namespace CommunityToolkit.Maui.Controls;

/// <summary>Avatar content view element interface.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IAvatarElement
{
	#region Public Property

	//// NOTE to implementer: Implement these properties as public.

	/// <summary>Gets a value indicating the avatar background colour.</summary>
	Color AvatarBackgroundColor { get; }

	/// <summary>Gets a value indicating the avatar border width.</summary>
	double BorderWidth { get; }

	/// <summary>Gets a value indicating the avatar corner radius (MAUI).</summary>
	CornerRadius CornerRadius { get; }

	/// <summary>Gets a value indicating the Avatar text.</summary>
	string Text { get; }

	/// <summary>Gets a value indicating the element width request.</summary>
	double AvatarWidthRequest { get; }

	/// <summary>Gets a value indicating the element height request.</summary>
	double AvatarHeightRequest { get; }

	#endregion Public Property

	#region Explicit methods

	//// NOTE to implementer: Implement these methods explicitly

	/// <summary>Gets a value indicating the corner radius default.</summary>
	CornerRadius CornerRadiusDefaultValue { get; }

	/// <summary>Gets a value indicating the text default.</summary>
	string TextDefaultValue { get; }

	/// <summary>Indicates whether avatar text is set.</summary>
	/// <returns>True if set.</returns>
	bool IsTextSet();

	/// <summary>On background colour property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnBackgroundColorChanged(Color oldValue, Color newValue);

	/// <summary>On border width property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnBorderWidthPropertyChanged(double oldValue, double newValue);

	/// <summary>On corner radius property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnCornerRadiusPropertyChanged(CornerRadius oldValue, CornerRadius newValue);

	/// <summary>On image source property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnImageSourceChanged(ImageSource oldValue, ImageSource newValue);

	/// <summary>On text property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnTextPropertyChanged(string oldValue, string newValue);

	/// <summary>On width request property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnWidthRequestPropertyChanged(double oldValue, double newValue);

	/// <summary>On height request property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnHeightRequestPropertyChanged(double oldValue, double newValue);

	#endregion Explicit methods
}