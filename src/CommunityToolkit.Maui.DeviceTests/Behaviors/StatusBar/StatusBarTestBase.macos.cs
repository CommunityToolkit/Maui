namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class StatusBarTestBase
{
	/// <summary>The message of the NotSupportedException that StatusBar.SetColor and SetStyle throw on this platform.</summary>
	protected const string unsupportedStatusBarMessage = "MacCatalyst does not currently support changing the macOS status bar color";

	// Mac Catalyst has no status bar code in the toolkit (StatusBar.macos.cs only throws): nothing to reset.
	protected partial Task ResetNativeStateAsync() => Task.CompletedTask;
}