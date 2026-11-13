using System.ComponentModel;
using System.Runtime.Versioning;

namespace CommunityToolkit.Maui.Core.Services;

/// <summary>
/// An Android-specific service to support <see cref="CommunityToolkit.Maui.Core.Platform.StatusBar"/> on modal pages
/// </summary>
/// <remarks>
/// When a dialog fragment of the activity starts, such as the one that .NET MAUI shows a modal page in, the window of its dialog gets the status bar
/// color of the activity's window and, from Android 11 on, its light status bars appearance. Whatever that color is, the dialog's window is laid out
/// like <see cref="CommunityToolkit.Maui.Core.Platform.StatusBar"/> lays out the activity's window: its content extends behind the system bars, and it
/// receives the window insets that a page uses to keep clear of them.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[SupportedOSPlatform("Android")]
public sealed partial class DialogFragmentService
{

}