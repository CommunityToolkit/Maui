namespace CommunityToolkit.Maui.DeviceTests;

// FileSaver, FolderPicker, Badge and SpeechToText (SpeechToText covers SpeechToTextImplementation, the online recognizers,
// OfflineSpeechToText covers OfflineSpeechToTextImplementation, the on-device recognizers), and the Android helpers that FileSaver and
// FolderPicker are built on.
public static partial class TestCategory
{
	// FileSaver: the shared result mapping plus the Android, iOS/Mac Catalyst and Windows implementations.
	public const string FileSaver = nameof(FileSaver);

	// FolderPicker: the shared result mapping plus the Android, iOS/Mac Catalyst and Windows implementations.
	public const string FolderPicker = nameof(FolderPicker);

	// Badge: Badge.Default/SetCount plus the Android, iOS/Mac Catalyst and Windows implementations and the Android BadgeFactory.
	public const string Badge = nameof(Badge);

	// Android only: the AndroidX ActivityResultRegistry wrapper that FileSaver and FolderPicker launch their pickers through.
	public const string ActivityResultManager = nameof(ActivityResultManager);

	// Android only: Uri.ToPhysicalPath(), AndroidPathExtensions.GetExternalDirectory(), AndroidStorageConstants and AndroidRequestCode.
	public const string AndroidStorageHelpers = nameof(AndroidStorageHelpers);

	public const string SpeechToText = nameof(SpeechToText);

	public const string OfflineSpeechToText = nameof(OfflineSpeechToText);
}