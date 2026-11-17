namespace CommunityToolkit.Maui.Sample.Views;

public partial class CodeEntryView : ContentView
{
	public static readonly BindableProperty CodeProperty = BindableProperty.Create(
		nameof(Code),
		typeof(string),
		typeof(CodeEntryView),
		string.Empty,
		BindingMode.TwoWay);

	public CodeEntryView()
	{
		InitializeComponent();
	}

	public string Code
	{
		get => (string)GetValue(CodeProperty);
		set => SetValue(CodeProperty, value);
	}
}