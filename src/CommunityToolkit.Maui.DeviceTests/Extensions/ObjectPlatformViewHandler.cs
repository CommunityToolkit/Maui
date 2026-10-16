using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// A view handler whose platform view is any object, so tests can hand toolkit code a platform view of an unexpected type (or no
/// platform view at all) and see how it reacts. Commands sent to the handler are recorded.
/// </summary>
/// <remarks>
/// Used by the KeyboardExtensions and SemanticOrderView tests. Every device-test type lives in the single namespace
/// CommunityToolkit.Maui.DeviceTests, so a helper shared by several features is named after what it is.
/// </remarks>
sealed class ObjectPlatformViewHandler(object? platformView, IMauiContext? mauiContext = null) : IViewHandler
{
	readonly List<(string Command, object? Args)> commands = [];

	/// <summary>A copy of every command sent to the handler, in order (commands may arrive from any thread).</summary>
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

	/// <summary>The arguments of every Focus command sent to the handler (MAUI also sends others, such as InvalidateMeasure).</summary>
	public IReadOnlyList<object?> FocusRequests => [.. Commands.Where(static command => command.Command == nameof(IView.Focus)).Select(static command => command.Args)];

	/// <summary>Called for every <see cref="Invoke"/>, after the command was recorded.</summary>
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