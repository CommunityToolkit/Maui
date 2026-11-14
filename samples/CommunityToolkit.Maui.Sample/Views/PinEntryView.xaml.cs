namespace CommunityToolkit.Maui.Sample.Views;

// Pin is deliberately a plain CLR property, not a BindableProperty, so the page can show a compiled binding on ValidationBehavior.Value
public partial class PinEntryView : ContentView
{
	public PinEntryView()
	{
		InitializeComponent();
	}

	public string Pin
	{
		get;
		private set
		{
			if (field == value)
			{
				return;
			}

			field = value;
			OnPropertyChanged();
		}
	} = string.Empty;

	void OnEntryTextChanged(object? sender, TextChangedEventArgs e) => Pin = e.NewTextValue ?? string.Empty;
}