using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.DeviceTests;

// Lets tests hand toolkit code a platform view of an unexpected type, or none at all.
sealed class ObjectPlatformViewHandler(object? platformView, IMauiContext? mauiContext = null) : IViewHandler
{
	// Commands may arrive from any thread.
	readonly List<(string Command, object? Args)> commands = [];

	public IReadOnlyList<(string Command, object? Args)> Commands
	{
		get
		{
			lock (commands)
			{
				return [.. commands];
			}
		}
	}

	public IReadOnlyList<object?> FocusRequests => [.. Commands.Where(static command => command.Command == nameof(IView.Focus)).Select(static command => command.Args)];

	public Action<string, object?>? OnInvoke { get; set; }

	public object? PlatformView { get; } = platformView;

	public IElement? VirtualView { get; private set; }

	public IMauiContext? MauiContext { get; private set; } = mauiContext;

	public bool HasContainer { get; set; }

	public object? ContainerView => null;

	IView? IViewHandler.VirtualView => VirtualView as IView;

	public Size GetDesiredSize(double widthConstraint, double heightConstraint) => Size.Zero;

	public void PlatformArrange(Rect frame)
	{
	}

	public void SetMauiContext(IMauiContext mauiContext) => MauiContext = mauiContext;

	public void SetVirtualView(IElement view) => VirtualView = view;

	public void UpdateValue(string property)
	{
	}

	public void Invoke(string command, object? args = null)
	{
		lock (commands)
		{
			commands.Add((command, args));
		}

		OnInvoke?.Invoke(command, args);
	}

	public void DisconnectHandler()
	{
	}
}