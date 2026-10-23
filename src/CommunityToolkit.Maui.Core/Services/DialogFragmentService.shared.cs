using System.ComponentModel;
using System.Runtime.Versioning;

namespace CommunityToolkit.Maui.Core.Services;

/// <summary>
/// An Android-specific service to support <see cref="CommunityToolkit.Maui.Core.Platform.StatusBar"/> on modal pages
/// </summary>
/// <remarks>
/// When the dialog of a modal page starts, its window gets the status bar color and the light status bars appearance of the activity's window.
/// Whatever that color is, the window is laid out like <see cref="CommunityToolkit.Maui.Core.Platform.StatusBar"/> lays out the activity's window:
/// its content extends behind the system bars, and it receives the window insets that a page uses to keep clear of them.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[SupportedOSPlatform("Android")]
public sealed partial class DialogFragmentService
{

}