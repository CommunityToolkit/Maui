namespace CommunityToolkit.Maui.Sample.Views;

public partial class NullableBoolComponentWithLabel : Label
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
		get => (bool?)GetValue(NullableIsVisibleProperty);
		set => SetValue(NullableIsVisibleProperty, value);
	}
}