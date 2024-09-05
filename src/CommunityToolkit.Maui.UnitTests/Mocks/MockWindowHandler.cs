namespace CommunityToolkit.Maui.UnitTests.Mocks;

class MockWindowHandler : IElementHandler
{
	public object? PlatformView { get; set; }
	public IElement? VirtualView { get; set; }

	public IMauiContext? MauiContext => new object() as IMauiContext;

	public void DisconnectHandler() { }

	public void Invoke(string command, object? args = null)
	{
		// No-op
	}

	public void SetMauiContext(IMauiContext mauiContext)
	{
		// No-op
	}

	public void SetVirtualView(IElement view) { }
	public void UpdateValue(string property) { }
}