using CommunityToolkit.Maui.DeviceTests.Runners;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.DeviceTestsInfrastructure)]
public sealed class CoverageRecorderTests : IDisposable
{
	readonly string resultsFolder = Path.Combine(Path.GetTempPath(), $"ctm-coverage-recorder-{Guid.NewGuid():N}");

	public CoverageRecorderTests()
	{
		Directory.CreateDirectory(resultsFolder);
	}

	public void Dispose()
	{
		Directory.Delete(resultsFolder, recursive: true);
	}

	[Fact]
	public void ReadDirectoryFile_ReturnsTheTrimmedFolderNamedNextToTheResultsFile()
	{
		const string hitsFolder = "hits-folder";
		File.WriteAllText(Path.Combine(resultsFolder, CoverageRecorder.DirectoryFileName), $"  {hitsFolder}\r\n");

		var directory = CoverageRecorder.ReadDirectoryFile(Path.Combine(resultsFolder, "TestResults.xml"));

		Assert.Equal(hitsFolder, directory);
	}

	[Fact]
	public void ReadDirectoryFile_ReturnsNullWhenNoFileExists()
	{
		var directory = CoverageRecorder.ReadDirectoryFile(Path.Combine(resultsFolder, "TestResults.xml"));

		Assert.Null(directory);
	}

	[Fact]
	public void ReadDirectoryFile_ReturnsNullWithoutAResultsFile()
	{
		Assert.Null(CoverageRecorder.ReadDirectoryFile(null));
		Assert.Null(CoverageRecorder.ReadDirectoryFile(string.Empty));
	}
}