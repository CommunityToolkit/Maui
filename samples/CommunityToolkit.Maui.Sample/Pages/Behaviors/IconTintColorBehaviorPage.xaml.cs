using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class IconTintColorBehaviorPage : BasePage<IconTintColorBehaviorViewModel>
{
	public IconTintColorBehaviorPage(IconTintColorBehaviorViewModel iconTintColorBehaviorViewModel)
		: base(iconTintColorBehaviorViewModel)
	{
		InitializeComponent();
	}

	void HandleButtonClicked(object? sender, EventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var button = (Button)sender;

		if (button.Behaviors.OfType<IconTintColorBehavior>().SingleOrDefault() is IconTintColorBehavior iconTintColorBehavior)
		{
			button.Behaviors.Remove(iconTintColorBehavior);
		}
		else
		{
			button.Behaviors.Add(new IconTintColorBehavior
			{
				TintColor = Colors.Green
			});
		}
	}
}