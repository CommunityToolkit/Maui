using CommunityToolkit.Maui.Converters;

namespace CommunityToolkit.Maui.DeviceTests;

// The target-type check of the converters no longer uses Activator or annotated generic parameters, so it must accept the same targets in a
// trimmed app and under NativeAOT. The binding passes the type of the target property as the target type.
[Category(TestCategory.BaseConverter)]
[Collection(TestCollections.DisableParallelization)]
public sealed class ConverterTargetTypeBindingTests : CommunityToolkitHandlerTestBase
{
	[Fact]
	public async Task InterfaceOutputConverterBoundToLabelTextShowsTheValue()
	{
		SetupBuilder();
		var checkBox = new CheckBox();
		var label = new Label();
		var converter = new BoolToObjectConverter<IFormattable> { TrueObject = 42, FalseObject = 7 };
		label.SetBinding(Label.TextProperty, BindingBase.Create(static (CheckBox source) => source.IsChecked, converter: converter, source: checkBox));

		await AttachAndRun(new VerticalStackLayout { checkBox, label }, async _ =>
		{
			await label.OnLoadedAsync();

			Assert.Equal("7", label.Text);

			checkBox.IsChecked = true;
			Assert.Equal("42", label.Text);
		});
	}

	[Fact]
	public async Task ValueTypeOutputConverterBoundToADoubleTargetConverts()
	{
		SetupBuilder();
		var checkBox = new CheckBox();
		var label = new Label();
		var converter = new BoolToObjectConverter<int> { TrueObject = 20, FalseObject = 10 };
		label.SetBinding(Label.FontSizeProperty, BindingBase.Create(static (CheckBox source) => source.IsChecked, converter: converter, source: checkBox));

		await AttachAndRun(new VerticalStackLayout { checkBox, label }, async _ =>
		{
			await label.OnLoadedAsync();

			Assert.Equal(10d, label.FontSize);

			checkBox.IsChecked = true;
			Assert.Equal(20d, label.FontSize);
		});
	}

	[Fact]
	public async Task ValueTypeOutputConverterBoundToANullableTargetConverts()
	{
		SetupBuilder();
		var checkBox = new CheckBox();
		var view = new NullableNumberView();
		var converter = new BoolToObjectConverter<int> { TrueObject = 20, FalseObject = 10 };
		view.SetBinding(NullableNumberView.NumberProperty, BindingBase.Create(static (CheckBox source) => source.IsChecked, converter: converter, source: checkBox));

		await AttachAndRun(new VerticalStackLayout { checkBox, view }, async _ =>
		{
			await view.OnLoadedAsync();

			Assert.Equal(10, view.Number);

			checkBox.IsChecked = true;
			Assert.Equal(20, view.Number);
		});
	}

	[Fact]
	public async Task MultiConverterChainBoundToLabelTextConverts()
	{
		SetupBuilder();
		var entry = new Entry { WidthRequest = 200 };
		var label = new Label();
		var converter = new MultiConverter
		{
			new IsStringNullOrEmptyConverter(),
			new BoolToObjectConverter<string> { TrueObject = "empty", FalseObject = "filled" }
		};
		label.SetBinding(Label.TextProperty, BindingBase.Create(static (Entry source) => source.Text, converter: converter, source: entry));

		await AttachAndRun(new VerticalStackLayout { entry, label }, async _ =>
		{
			await label.OnLoadedAsync();

			Assert.Equal("empty", label.Text);

			entry.Text = "abc";
			Assert.Equal("filled", label.Text);

			entry.Text = string.Empty;
			Assert.Equal("empty", label.Text);
		});
	}

	[Fact]
	public void IncompatibleTargetReturnsDefaultConvertReturnValueWhenExceptionsAreSuppressed()
	{
		SetupBuilder(configureToolkitOptions: static options => options.SetShouldSuppressExceptionsInConverters(true));
		var converter = new BoolToObjectConverter<int> { DefaultConvertReturnValue = 99, TrueObject = 20, FalseObject = 10 };

		var result = ((ICommunityToolkitValueConverter)converter).Convert(true, typeof(Uri), null, null);

		Assert.Equal(99, result);
	}

	[Fact]
	public void IncompatibleTargetThrowsAnArgumentExceptionWhenExceptionsAreNotSuppressed()
	{
		SetupBuilder(configureToolkitOptions: static options => options.SetShouldSuppressExceptionsInConverters(false));
		var converter = new BoolToObjectConverter<int> { TrueObject = 20, FalseObject = 10 };

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)converter).Convert(true, typeof(Uri), null, null));
	}

	sealed class NullableNumberView : ContentView
	{
		public static readonly BindableProperty NumberProperty = BindableProperty.Create(nameof(Number), typeof(int?), typeof(NullableNumberView), null);

		public int? Number
		{
			get => (int?)GetValue(NumberProperty);
			set => SetValue(NumberProperty, value);
		}
	}
}