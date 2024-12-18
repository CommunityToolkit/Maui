using AVFoundation;

namespace CommunityToolkit.Maui;

partial class MediaOptions
{
	/// <summary>
	/// Gets or sets the category for the audio session.
	/// </summary>
	public AVAudioSessionCategory Category { get; set; } = AVAudioSessionCategory.Record;
    
	/// <summary>
	/// Gets or sets the mode for the audio session.
	/// </summary>
	public AVAudioSessionMode Mode { get; set; } = default;
    
	/// <summary>
	/// Gets or sets the options for the audio session category.
	/// </summary>
	public AVAudioSessionCategoryOptions CategoryOptions { get; set; } = default;

	// /// <summary>
	// /// Gets or sets the lifetime of the underlying audio session - basically whether the AVAudioSession will stay active or be deactivated.
	// /// </summary>
	// public SessionLifetime SessionLifetime { get; set; } = default;
}