using CommunityToolkit.Maui.DeviceTests.Runners;
using CoreGraphics;
using Microsoft.Maui.Handlers;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class MauiWindowHostTestBase
{
	protected readonly record struct RunnerWindowGeometry(CGRect Frame, CGSize MinimumSize, CGSize MaximumSize)
	{
		public static RunnerWindowGeometry Capture()
		{
			var restrictions = RunnerScene.SizeRestrictions ?? throw new InvalidOperationException("The scene of the runner has no size restrictions.");
			return new(TestWindow.PlatformWindow.Frame, restrictions.MinimumSize, restrictions.MaximumSize);
		}

		public override string ToString() => $"frame {Frame}, minimum size {MinimumSize}, maximum size {MaximumSize}";
	}

	sealed class GuestWindowHandler : WindowHandler
	{
		static readonly IPropertyMapper<IWindow, IWindowHandler> guestMapper = new PropertyMapper<IWindow, IWindowHandler>(Mapper)
		{
			[nameof(IWindow.X)] = static (_, _) => { },
			[nameof(IWindow.Y)] = static (_, _) => { },
			[nameof(IWindow.Width)] = static (_, _) => { },
			[nameof(IWindow.Height)] = static (_, _) => { },
			[nameof(IWindow.MinimumWidth)] = static (_, _) => { },
			[nameof(IWindow.MinimumHeight)] = static (_, _) => { },
			[nameof(IWindow.MaximumWidth)] = static (_, _) => { },
			[nameof(IWindow.MaximumHeight)] = static (_, _) => { }
		};

		public GuestWindowHandler(IMauiContext windowContext)
			: base(guestMapper)
		{
			SetMauiContext(windowContext);
		}
	}
}