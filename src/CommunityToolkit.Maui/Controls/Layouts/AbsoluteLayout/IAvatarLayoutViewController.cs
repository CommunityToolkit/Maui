namespace CommunityToolkit.Maui.Controls;

/// <summary>Avatar layout view controller interface.</summary>
public interface IAvatarLayoutViewController : IViewController
{
	/// <summary>Send clicked.</summary>
	void SendClicked();

	/// <summary>Send pressed.</summary>
	void SendPressed();

	/// <summary>Send released.</summary>
	void SendReleased();
}