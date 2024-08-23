using System.Reflection;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class RatingViewXamlPage : BasePage<RatingViewXamlViewModel>
{
	readonly IReadOnlyDictionary<string, Color> colors = typeof(Colors).GetFields(BindingFlags.Static | BindingFlags.Public).ToDictionary(c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()));

	public RatingViewXamlPage(RatingViewXamlViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		ColorPickerEmptyBackground.ItemsSource = colors.Keys.ToList();
	}

	void ColorPickerEmptyBackground_SelectedIndexChanged(object sender, EventArgs e)
	{
		Color color = colors.ElementAtOrDefault(ColorPickerEmptyBackground.SelectedIndex).Value ?? Colors.Transparent;
		ColorPickerEmptyBackgroundRatingView.EmptyBackgroundColor = color;
	}

	void StepperValueMaximumRatings_RatingChanged(object sender, EventArgs e)
	{
		// This is the weak event raised when the rating is changed, so that the developer can use to then perform further actions (such as save to DB).
		if (sender is RatingView ratingView)
		{
			StepperValueMaximumRatingsCurrentRanking.Text = ratingView.CurrentRating.ToString();
		}
	}
}