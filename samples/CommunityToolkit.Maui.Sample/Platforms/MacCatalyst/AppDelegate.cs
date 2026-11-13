using System.Diagnostics.CodeAnalysis;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Sample;

[Register(nameof(AppDelegate))]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override void BuildMenu(IUIMenuBuilder builder)
	{
		base.BuildMenu(builder);

		// UIApplicationSupportsMultipleScenes lets MediaElementMultipleWindowsPage open its second window, and it adds File > New Window.
		// The Sample has one main window (see SceneDelegate.WillConnect), so the menu bar does not offer another one.
		var newWindowMenuIdentifier = OperatingSystem.IsMacCatalystVersionAtLeast(26)
			? UIMenuIdentifier.NewItem.GetConstant()
			: UIMenuIdentifier.NewScene.GetConstant();

		builder.RemoveMenu(newWindowMenuIdentifier?.ToString() ?? throw new InvalidOperationException("File > New Window has no menu identifier"));
	}
}