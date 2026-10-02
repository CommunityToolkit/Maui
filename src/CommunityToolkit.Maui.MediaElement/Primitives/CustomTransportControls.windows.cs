using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace CommunityToolkit.Maui.Primitives;

sealed partial class CustomTransportControls : MediaTransportControls
{
	public AppBarButton FullScreenButton = new();

	public CustomTransportControls()
	{
		this.DefaultStyleKey = typeof(CustomTransportControls);
	}

	public event EventHandler<EventArgs>? OnTemplateLoaded;

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		if (GetTemplateChild("FullWindowButton") is AppBarButton appBarButton)
		{
			FullScreenButton = appBarButton;
			FullScreenButton.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
			OnTemplateLoaded?.Invoke(this, EventArgs.Empty);
		}
	}
}