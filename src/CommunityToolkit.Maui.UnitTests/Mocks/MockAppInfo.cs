namespace CommunityToolkit.Maui.UnitTests.Mocks;

class MockAppInfo : IAppInfo
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public string PackageName { get; set; }

	public string Name { get; set; }

	public string VersionString { get; set; }

	public Version Version { get; set; }

	public string BuildString { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public LayoutDirection RequestedLayoutDirection { get; set; }

	public void ShowSettingsUI()
	{
	}

	public AppTheme RequestedTheme { get; set; }

	public AppPackagingModel PackagingModel { get; set; }
}