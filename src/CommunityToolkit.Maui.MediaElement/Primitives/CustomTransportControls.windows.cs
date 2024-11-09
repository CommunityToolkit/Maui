using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace CommunityToolkit.Maui.Primitives;

sealed partial class CustomTransportControls : MediaTransportControls
{
	public event EventHandler<EventArgs>? OnTemplateLoaded;
	public AppBarButton FullScreenButton = new();
	bool isFullScreen = false;

	public CustomTransportControls()
	{
		this.DefaultStyleKey = typeof(CustomTransportControls);
	}

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
