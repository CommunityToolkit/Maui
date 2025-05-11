namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class SimplePopup : ContentView, IQueryAttributable
{
	public SimplePopup()
	{
		InitializeComponent();
	}

	void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
	{
		DescriptionLabel.Text = (string)query[nameof(DescriptionLabel)];
	}
}