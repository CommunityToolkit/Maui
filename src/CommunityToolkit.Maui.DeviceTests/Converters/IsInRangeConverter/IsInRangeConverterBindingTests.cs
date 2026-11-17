using CommunityToolkit.Maui.Converters;

namespace CommunityToolkit.Maui.DeviceTests;

// A converter over value types is the case where trimming and NativeAOT need the parameterless constructors of TValue and TReturnObject kept.
[Category(TestCategory.IsInRangeConverter)]
[Collection(TestCollections.DisableParallelization)]
public sealed class IsInRangeConverterBindingTests : CommunityToolkitHandlerTestBase
{
	[Fact]
	public async Task BindingConvertsASliderValueInsideAndOutsideTheRangeToABoolean()
	{
		SetupBuilder();
		var slider = new Slider { Minimum = 0, Maximum = 100 };
		var label = new Label();
		label.SetBinding(Label.IsVisibleProperty, BindingBase.Create(static (Slider source) => source.Value, converter: new IsInRangeConverter { MinValue = 10d, MaxValue = 20d }, source: slider));

		await AttachAndRun(new VerticalStackLayout { slider, label }, async _ =>
		{
			await label.OnLoadedAsync();

			slider.Value = 15;
			Assert.True(label.IsVisible);

			slider.Value = 50;
			Assert.False(label.IsVisible);
		});
	}

	[Fact]
	public async Task BindingReturnsTrueObjectAndFalseObjectOfTheConverter()
	{
		SetupBuilder();
		var entry = new Entry { WidthRequest = 200 };
		var label = new Label();
		var converter = new IsInRangeConverter
		{
			MinValue = 3,
			MaxValue = 5,
			TrueObject = "in range",
			FalseObject = "out of range"
		};
		label.SetBinding(Label.TextProperty, BindingBase.Create(static (Entry source) => source.Text.Length, converter: converter, source: entry));

		await AttachAndRun(new VerticalStackLayout { entry, label }, async _ =>
		{
			await label.OnLoadedAsync();

			entry.Text = "abcd";
			Assert.Equal("in range", label.Text);

			entry.Text = "ab";
			Assert.Equal("out of range", label.Text);
		});
	}

	[Fact]
	public async Task BindingOfAConverterOverValueTypesReturnsTrueObjectAndFalseObject()
	{
		SetupBuilder();
		var levelView = new LevelView();
		var label = new Label();
		var inRange = new Thickness(1, 2, 3, 4);
		var outOfRange = new Thickness(9);
		var converter = new LevelRangeConverter { MinValue = new Level(10), MaxValue = new Level(20), TrueObject = inRange, FalseObject = outOfRange };
		label.SetBinding(Label.PaddingProperty, BindingBase.Create(static (LevelView source) => source.Level, converter: converter, source: levelView));

		await AttachAndRun(new VerticalStackLayout { levelView, label }, async _ =>
		{
			await label.OnLoadedAsync();

			levelView.Level = new Level(15);
			Assert.Equal(inRange, label.Padding);

			levelView.Level = new Level(50);
			Assert.Equal(outOfRange, label.Padding);
		});
	}

	[Fact]
	public void ConverterOverValueTypesHasDefaultMinValueAndMaxValue()
	{
		var converter = new LevelRangeConverter();

		Assert.Equal(default, converter.MinValue);
		Assert.Equal(default, converter.MaxValue);
		Assert.Equal(default, converter.TrueObject);
		Assert.Equal(default, converter.FalseObject);
	}

	internal readonly record struct Level(double Number) : IComparable
	{
		public int CompareTo(object? obj) => obj is Level other ? Number.CompareTo(other.Number) : 1;
	}

	sealed class LevelRangeConverter : IsInRangeConverter<Level, Thickness>
	{
	}

	internal sealed class LevelView : ContentView
	{
		public static readonly BindableProperty LevelProperty = BindableProperty.Create(nameof(Level), typeof(Level), typeof(LevelView), default(Level));

		public Level Level
		{
			get => (Level)GetValue(LevelProperty);
			set => SetValue(LevelProperty, value);
		}
	}
}