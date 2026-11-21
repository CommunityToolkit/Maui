using System.Windows.Input;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Sample;

/// <summary>
/// 
/// </summary>
public partial class PlatformBarcodeScanningScenario : PlatformCameraScenario
{
	public ICommand? Command { get; set; }
}