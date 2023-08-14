namespace CommunityToolkit.Maui.UnitTests.Mocks;

class MockAppInfo : IAppInfo
{
	public string PackageName { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string VersionString { get; set; } = string.Empty;

	public string BuildString { get; set; } = string.Empty;

	public Version Version { get; set; } = new Version(1, 0);

	public LayoutDirection RequestedLayoutDirection { get; set; }

	public AppTheme RequestedTheme { get; set; }

	public AppPackagingModel PackagingModel { get; set; }

	public void ShowSettingsUI()
	{
	}
}