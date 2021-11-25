using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views.Popup;
using CommunityToolkit.Maui.Views.Popup.SnackBar;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockSnackbar : ISnackbar
{
	public Action Action { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public string ActionButtonText { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public IView? Anchor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public TimeSpan Duration { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public bool IsShown { get; private set; }

	public string Text { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public SnackbarOptions VisualOptions { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public event EventHandler? Dismissed;
	public event EventHandler<ShownEventArgs>? Shown;

	public Task Dismiss()
	{
		IsShown = false;
		return Task.CompletedTask;
	}

	public Task Show()
	{
		IsShown = true;
		return Task.CompletedTask;
	}
}