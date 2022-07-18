namespace CommunityToolkit.Maui.Core;

/// <summary>Avatar view interface.</summary>
public interface IAvatarView : IBorderView, ILabel, Microsoft.Maui.IImage, IImageSource
{
	//// NOTE to implementer: Implement the following properties as public.

	/// <summary>Gets a value indicating the avatar border colour.</summary>
	Color BorderColor { get; }

	/// <summary>Gets a value indicating the avatar border width.</summary>
	double BorderWidth { get; }

	/// <summary>Gets a value indicating the avatar corner radius <see cref="Microsoft.Maui.CornerRadius"/>.</summary>
	CornerRadius CornerRadius { get; }

	//// NOTE to implementer: Implement these methods explicitly

	/// <summary>Gets a value indicating the text default.</summary>
	string TextDefaultValue { get; }

	/// <summary>Indicates whether border colour is set.</summary>
	/// <returns>True if set.</returns>
	bool IsBorderColorSet();

	/// <summary>Indicates whether border width is set.</summary>
	/// <returns>True if set.</returns>
	bool IsBorderWidthSet();

	/// <summary>Indicates whether corner radius is set.</summary>
	/// <returns>True if set.</returns>
	bool IsCornerRadiusSet();

	/// <summary>Indicates whether image source is set.</summary>
	/// <returns>True if set.</returns>
	bool IsImageSourceSet();

	/// <summary>Indicates whether avatar text is set.</summary>
	/// <returns>True if set.</returns>
	bool IsTextSet();

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

	/// <summary>Padding default value creator method.</summary>
	/// <returns>Padding thickness.</returns>
	Thickness PaddingDefaultValueCreator();

	/// <summary>On border colour property changed.</summary>
	/// <param name="oldValue">Old value</param>
	/// <param name="newValue">New Value</param>
	void OnBorderColorPropertyChanged(Color oldValue, Color newValue);
}