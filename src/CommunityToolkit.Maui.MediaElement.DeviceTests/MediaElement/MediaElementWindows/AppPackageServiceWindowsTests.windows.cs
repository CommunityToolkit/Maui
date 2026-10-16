using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.DeviceTests;

// The CI runs the whole app packaged (MSIX) and unpackaged, so each run takes exactly one branch of the Lazy initialisers and asserts that
// branch against an independent oracle (the Win32 package API); together the two runs cover both branches.
[Category(TestCategory.AppPackageServiceWindows)]
[Collection(TestCollections.DisableParallelization)]
public class AppPackageServiceWindowsTests : WinMediaTestBase
{
	[Fact]
	public void IsPackagedAppMatchesTheProcessPackaging()
	{
		Assert.Equal(WinMediaPackaging.IsPackaged, AppPackageService.IsPackagedApp);
	}

	[Fact]
	public void FullAppPackageFilePathMatchesThePackagingMode()
	{
		var expected = WinMediaPackaging.ExpectedPackageFilePath;

		Assert.Equal(expected.TrimEnd(Path.DirectorySeparatorChar), AppPackageService.FullAppPackageFilePath.TrimEnd(Path.DirectorySeparatorChar), ignoreCase: true);
	}

	[Fact]
	public void FullAppPackageFilePathContainsTheBundledTestAssets()
	{
		// ResourceMediaSource resolves its assets below this folder, so it must be where MauiAsset files are deployed in both modes.
		Assert.True(Directory.Exists(AppPackageService.FullAppPackageFilePath), AppPackageService.FullAppPackageFilePath);
		Assert.True(File.Exists(Path.Combine(AppPackageService.FullAppPackageFilePath, "mediaplayer", "video.mp4")),
			$"mediaplayer/video.mp4 was not found below {AppPackageService.FullAppPackageFilePath}");
	}
}