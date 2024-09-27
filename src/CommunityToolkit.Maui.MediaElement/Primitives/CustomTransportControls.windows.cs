using CommunityToolkit.Maui.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Button = Microsoft.UI.Xaml.Controls.Button;
namespace CommunityToolkit.Maui.Primitives;

/// <summary>
/// A class that provides a way to create custom transport controls for a <see cref="IMediaElement"/>.
/// </summary>
public sealed class CustomTransportControls : MediaTransportControls
{
	/// <summary>
	/// An event that triggers when the template is loaded.
	/// </summary>
	public event EventHandler<EventArgs>? OnTemplateLoaded;

	/// <summary>
	/// A button for Full screen controls.
	/// </summary>
	public AppBarButton? FullScreenButton { get; private set; }
	bool isFullScreen = false;

	/// <summary>
	/// A <see cref="CustomTransportControls"/> for a <see cref="IMediaElement"/>.
	/// </summary>
	public CustomTransportControls()
	{
		this.DefaultStyleKey = typeof(CustomTransportControls);
	}

	/// <summary>
	/// Add full screen button to the template.
	/// </summary>
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		var temp = GetTemplateChild("FullWindowButton") as AppBarButton;
		if(temp is not null)
		{
			FullScreenButton = temp;
			FullScreenButton.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
			OnTemplateLoaded?.Invoke(this, EventArgs.Empty);
			FullScreenButton.Click += FullScreenButton_Click;
		}
	}

	void FullScreenButton_Click(object sender, RoutedEventArgs e)
	{
		if (FullScreenButton is null)
		{
			return;
		}
		if (isFullScreen)
		{
			FullScreenButton.Icon = new FontIcon { Glyph = "\uE740" };
			isFullScreen = false;
		}
		else
		{
			FullScreenButton.Icon = new SymbolIcon(Symbol.BackToWindow);
			isFullScreen = true;
		}
	}
}
