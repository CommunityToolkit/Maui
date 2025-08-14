using System.Windows.Input;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Sample;

/// <summary>
/// 
/// </summary>
public partial class PlatformBarcodeScanningScenario : PlatformCameraScenario
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="Command"/> property.
	/// </summary>
	public static readonly BindableProperty CommandProperty =
		BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(PlatformBarcodeScanningScenario));
	
	/// <summary>
	/// The Command that should be executed when a barcode is detected.
	/// </summary>
	public ICommand? Command
	{
		get => (ICommand?)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}
}