namespace CommunityToolkit.Maui.DeviceTests;

// The platform behaviors (TouchBehavior with GestureManager, IconTintColorBehavior, SelectAllTextBehavior, ImageTouchBehavior,
// StatusBarBehavior and their base class), and what StatusBarBehavior is built on: CommunityToolkit.Maui.Core.Platform.StatusBar (Android
// and iOS, the "not supported" throws of Mac Catalyst and Windows). The Android window lookup that StatusBar uses is in Extensions.
// The validation behaviors (ValidationBehavior and the behaviors built on it) on the platform view of an Entry.
// Behaviors of an InputView that react to text from its platform view: MaskedBehavior, and the keyboard dismissal of MaxLengthReachedBehavior
// and UserStoppedTypingBehavior.
public static partial class TestCategory
{
	public const string BasePlatformBehavior = nameof(BasePlatformBehavior);

	public const string IconTintColorBehavior = nameof(IconTintColorBehavior);

	public const string ImageTouchBehavior = nameof(ImageTouchBehavior);

	public const string MaskedBehavior = nameof(MaskedBehavior);

	public const string MaxLengthReachedBehavior = nameof(MaxLengthReachedBehavior);

	public const string SelectAllTextBehavior = nameof(SelectAllTextBehavior);

	public const string StatusBarBehavior = nameof(StatusBarBehavior);

	public const string StatusBar = nameof(StatusBar);

	public const string TouchBehavior = nameof(TouchBehavior);

	public const string UserStoppedTypingBehavior = nameof(UserStoppedTypingBehavior);

	public const string ValidationBehavior = nameof(ValidationBehavior);
}