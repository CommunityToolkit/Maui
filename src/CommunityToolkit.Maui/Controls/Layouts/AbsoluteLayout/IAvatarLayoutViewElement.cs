using System.ComponentModel;
using System.Windows.Input;

namespace CommunityToolkit.Maui.Controls.Layouts.AbsoluteLayout;

/// <summary>Avatar view element interface.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IAvatarLayoutViewElement
{
	#region Public Property

	//// NOTE to implementer: Implement these properties as public.

	/// <summary>Gets a value indicating the Avatar background colour.</summary>
	Color AvatarBackgroundColor { get; }

	/// <summary>Gets a value indicating the Avatar corner radius.</summary>
	int AvatarCornerRadius { get; }

	/// <summary>Gets a value indicating the Avatar height request.</summary>
	double AvatarHeightRequest { get; }

	/// <summary>Gets a value indicating the avatar shadow.</summary>
	Shadow AvatarShadow { get; }

	/// <summary>Gets a value indicating the avatar width request.</summary>
	double AvatarWidthRequest { get; }

	/// <summary>Gets a value indicating the Avatar text.</summary>
	string Text { get; }

	/// <summary>Gets a value indicating the command parameter.</summary>
	object CommandParameter { get; set; }

	/// <summary>Gets a value indicating the command ICommand.</summary>
	ICommand Command { get; set; }

	/// <summary>Gets a value indicating the avatar is pressed.</summary>
	bool IsPressed { get; }

	#endregion Public Property

	#region Explicit methods

	//// NOTE to implementer: Implement these methods explicitly

	/// <summary>Gets a value indicating the avatar default background colour.</summary>
	Color AvatarBackgroundColorDefault { get; }

	/// <summary>Gets a value indicating the avatar default corder radius.</summary>
	int AvatarCorderRadiusDefault { get; }

	/// <summary>Gets a value indicating the avatar default height request.</summary>
	double AvatarHeightRequestDefault { get; }

	/// <summary>Gets a value indicating the avatar default shadow.</summary>
	Shadow AvatarShadowDefault { get; }

	/// <summary>Gets a value indicating the avatar default width request.</summary>
	double AvatarWidthRequestDefault { get; }

	/// <summary>Gets a value indicating the text default.</summary>
	string TextDefaultValue { get; }

	/// <summary>Indicates whether avatar background colour is set.</summary>
	/// <returns>True if set.</returns>
	bool IsAvatarBackgroundColorSet();

	/// <summary>Indicates whether avatar corner radius is set.</summary>
	/// <returns>True if set.</returns>
	bool IsAvatarCornerRadiusSet();

	/// <summary>Indicates whether the avatar height request is set.</summary>
	/// <returns>True if set.</returns>
	bool IsAvatarHeightRequestSet();

	/// <summary>Indicates whether the avatar shadow is set.</summary>
	/// <returns>True if the shadow is set.</returns>
	bool IsAvatarShadowSet();

	/// <summary>Indicates whether the avatar width request is set.</summary>
	/// <returns>True if set.</returns>
	bool IsAvatarWidthRequestSet();

	/// <summary>Indicates whether avatar text is set.</summary>
	/// <returns>True if set.</returns>
	bool IsTextSet();

	/// <summary>On avatar background colour property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnAvatarBackgroundColorPropertyChanged(Color oldValue, Color newValue);

	/// <summary>On avatar corner radius property set.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnAvatarCornerRadiusPropertyChanged(int oldValue, int newValue);

	/// <summary>On avatar height request property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnAvatarHeightRequestPropertyChanged(double oldValue, double newValue);

	/// <summary>On avatar shadow property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnAvatarShadowPropertyChanged(Shadow oldValue, Shadow newValue);

	/// <summary>On avatar width request property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnAvatarWidthRequestPropertyChanged(double oldValue, double newValue);

	/// <summary>On text property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnTextPropertyChanged(string oldValue, string newValue);

	/// <summary>Propagate up clicked.</summary>
	void PropagateUpClicked();

	/// <summary>Propagate up pressed.</summary>
	void PropagateUpPressed();

	/// <summary>Propagate up released.</summary>
	void PropagateUpReleased();

	/// <summary>Set is pressed.</summary>
	void SetIsPressed(bool isPressed);

	/// <summary>On command can execute changed.</summary>
	/// <param name="sender">Object sender.</param>
	/// <param name="e">Event arguments.</param>
	void OnCommandCanExecuteChanged(object? sender, EventArgs e);

	/// <summary>Gets a value indicating whether is enabled at core.</summary>
	bool IsEnabledCore { set; }

	#endregion Explicit methods
}