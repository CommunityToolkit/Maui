using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace CommunityToolkit.Maui.BuildTasks;

public partial class VerifyMauiWorkloadVersionTask : Microsoft.Build.Utilities.Task
{
	static readonly ProcessStartInfo processStartInfo = new()
	{
		FileName = "dotnet",
		Arguments = "workload list",
		UseShellExecute = false,
		RedirectStandardOutput = true,
		CreateNoWindow = true,
		WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
	};

	[Required]
	public string MinimumRequiredMauiVersion { get; set; } = string.Empty;

	public override bool Execute()
	{
		try
		{
			var dotnetOutput = GetDotnetWorkloadOutput();
			var installedMauiWorkloadVersion = ExtractMauiVersion(dotnetOutput);

			if (string.IsNullOrEmpty(installedMauiWorkloadVersion))
			{
				Log.LogError($"No MAUI workload installed/n{GenerateTroubleshootingText(MinimumRequiredMauiVersion)}");
				return false;
			}

			var currentVersion = Version.Parse(installedMauiWorkloadVersion);
			var minimumRequiredVersion = Version.Parse(MinimumRequiredMauiVersion);

			if (currentVersion < minimumRequiredVersion)
			{
				Log.LogError($"The installed MAUI workload version, {installedMauiWorkloadVersion}, does not meet the minimum version required by the .NET MAUI Community Toolkit: {MinimumRequiredMauiVersion}./n{GenerateTroubleshootingText(MinimumRequiredMauiVersion)}");
				return false;
			}

			Log.LogMessage(MessageImportance.Normal, $"MAUI workload version {installedMauiWorkloadVersion} satisfies the required version {MinimumRequiredMauiVersion}.");

			return true;
		}
		catch (Exception ex)
		{
			Log.LogErrorFromException(ex);
			return false;
		}
	}

	static string GetDotnetWorkloadOutput()
	{
		using var process = new Process();
		process.StartInfo = processStartInfo;

		process.Start();
		var output = process.StandardOutput.ReadToEnd();
		process.WaitForExit();

		return output;
	}

	static string? ExtractMauiVersion(string dotnetOutput)
	{
		var match = VerifyWorkloadVersion().Match(dotnetOutput);

		return match.Success ? match.Groups[0].Value : null;
	}

	static string GenerateTroubleshootingText(in string minimumMauiVersion) =>
		$"""
		Follow these steps to install the required .NET MAUI workload:
			1. Using your web browser, download the latest stable release of the .NET SDK and install it on your machine: https://dotnet.microsoft.com/download/dotnet
			2. After installing the latest version of .NET, using your console, use the command `dotnet workload install maui --version {minimumMauiVersion}` to update to the latest stable version of .NET MAUI \n
		""";
	
	[GeneratedRegex(@"(?<=Workload version: )\d+\.\d+\.\d+\.\d+")]
	private static partial Regex VerifyWorkloadVersion();
}