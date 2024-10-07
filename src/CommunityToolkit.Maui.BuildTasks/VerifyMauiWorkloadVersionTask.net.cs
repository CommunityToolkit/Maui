namespace CommunityToolkit.Maui.BuildTasks;

/// <summary>
/// An MSBuild task to ensure that developers have the required version of .NET MAUI Workload installed
/// </summary>
public class VerifyMauiWorkloadVersionTask : Microsoft.Build.Utilities.Task
{
	/// <inheritdoc/>
	public override bool Execute() => false;
}