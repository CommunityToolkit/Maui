using System.ComponentModel;

// TODO: Fix problem with not finding assembly reference for Microsoft.Maui.Controls.
//using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Core;

/// <summary>Avatar view interface.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IAvatarView : IContentView, IText, IImageSource, IImageSourcePart
{
	#region Public Property

	//// NOTE to implementer: Implement these properties as public.

	/// <summary>Gets a value indicating the avatar background colour.</summary>
	Color AvatarBackgroundColor { get; }

	/// <summary>Gets a value indicating the avatar border colour.</summary>
	Color BorderColor { get; }

	/// <summary>Gets a value indicating the avatar border width.</summary>
	double BorderWidth { get; }

	/// <summary>Gets a value indicating the avatar corner radius (MAUI).</summary>
	CornerRadius CornerRadius { get; }

	/// <summary>Gets a value indicating the element width request.</summary>
	double AvatarWidthRequest { get; }

	/// <summary>Gets a value indicating the element height request.</summary>
	double AvatarHeightRequest { get; }

	/// <summary>Gets a value indicating the avatar padding.</summary>
	Thickness AvatarPadding { get; }

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
	void OnImageSourceChanged(object oldValue, object newValue);

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

	/// <summary>On padding property changed</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnPaddingPropertyChanged(Thickness oldValue, Thickness newValue);

	/// <summary>Padding default value creator method.</summary>
	/// <returns>Padding thickness.</returns>
	Thickness PaddingDefaultValueCreator();

	/// <summary>On border colour property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnBorderColorPropertyChanged(Color oldValue, Color newValue);

	/// <summary>On avatar shadow property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnShadowPropertyChanged(object oldValue, object newValue);

	#endregion Explicit methods
}