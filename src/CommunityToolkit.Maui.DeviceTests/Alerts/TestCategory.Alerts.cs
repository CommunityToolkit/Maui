namespace CommunityToolkit.Maui.DeviceTests;

// Snackbar and Toast on every platform, the iOS and Mac Catalyst alert view stack, the Windows Snackbar setup in Options, and the
// Android NavigationBar remap done by UseMauiCommunityToolkit.
public static partial class TestCategory
{
	public const string Snackbar = nameof(Snackbar);

	public const string Toast = nameof(Toast);

	public const string AlertView = nameof(AlertView);

	public const string SnackbarWindowsSetup = nameof(SnackbarWindowsSetup);

	public const string AppBuilderNavigationBarRemap = nameof(AppBuilderNavigationBarRemap);
}