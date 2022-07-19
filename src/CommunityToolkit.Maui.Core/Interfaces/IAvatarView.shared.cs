namespace CommunityToolkit.Maui.Core;

/// <summary>Avatar view interface.</summary>
public interface IAvatarView : IBorderView, ILabel, Microsoft.Maui.IImage, IImageSource
{
	/// <summary>Gets a value indicating the avatar border colour.</summary>
	Color BorderColor { get; }

	/// <summary>Gets a value indicating the avatar border width.</summary>
	double BorderWidth { get; }

	/// <summary>Gets a value indicating the avatar corner radius <see cref="Microsoft.Maui.CornerRadius"/>.</summary>
	CornerRadius CornerRadius { get; }
}