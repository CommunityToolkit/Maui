namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class NullableBoolComponentWithLabel : ContentView
{
	public static readonly BindableProperty NullableIsVisibleProperty = BindableProperty.Create(
		nameof(NullableIsVisible),
		typeof(bool?),
		typeof(NullableBoolComponentWithLabel));


	public NullableBoolComponentWithLabel()
	{
		InitializeComponent();
	}

	public bool? NullableIsVisible
	{
		get => (bool?) GetValue(NullableIsVisibleProperty);
		set => SetValue(NullableIsVisibleProperty, value);
	}
}
