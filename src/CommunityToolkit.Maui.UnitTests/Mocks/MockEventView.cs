using System;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockEventView : View
{
	public event EventHandler? Event;

	public void InvokeEvent() => Event?.Invoke(this, EventArgs.Empty);
}