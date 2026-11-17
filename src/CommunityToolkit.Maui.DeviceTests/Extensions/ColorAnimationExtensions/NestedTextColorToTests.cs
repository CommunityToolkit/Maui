using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.DeviceTests;

// The TextColorTo generator used to fail with CS0234 for a text view nested in another class, so building this app proves that the
// nested output compiles. The tests prove that the generated animations run on the platform's animation ticker.
[Category(TestCategory.ColorAnimationExtensions)]
[Collection(TestCollections.DisableParallelization)]
public class NestedTextColorToTests : CommunityToolkitHandlerTestBase
{
	const string textColorToAnimationName = "TextColorTo";

	const uint animationLength = 100;

	[Fact]
	public async Task GeneratedTextColorToAnimatesTheTextColorOfATextViewNestedInAnotherClass()
	{
		var view = new TextViewContainer.NestedTextView();

		await AssertTextColorToAnimatesAsync(view, view.ChildLabel, () => view.TextColor, color => view.TextColorTo(color, rate: 1, length: animationLength));
	}

	[Fact]
	public async Task GeneratedTextColorToAnimatesTheTextColorOfAnInternalTextViewNestedTwoLevelsDeep()
	{
		var view = new TextViewContainer.Middle.DeeplyNestedTextView();

		await AssertTextColorToAnimatesAsync(view, view.ChildLabel, () => view.TextColor, color => view.TextColorTo(color, rate: 1, length: animationLength));
	}

	[Fact]
	public async Task GeneratedTextColorToAnimatesTheTextColorOfATextViewNestedInAGenericClass()
	{
		var view = new GenericTextViewContainer<int>.NestedTextView();

		await AssertTextColorToAnimatesAsync(view, view.ChildLabel, () => view.TextColor, color => view.TextColorTo(color, rate: 1, length: animationLength));
	}

	[Fact]
	public async Task GeneratedTextColorToOfANestedTextViewThrowsForACanceledToken()
	{
		SetupBuilder();
		var view = new TextViewContainer.NestedTextView();
		using var cancellationTokenSource = new CancellationTokenSource();
		await cancellationTokenSource.CancelAsync();

		await AttachAndRun(view, async _ =>
		{
			await view.OnLoadedAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => view.TextColorTo(Colors.Red, rate: 1, length: animationLength, token: cancellationTokenSource.Token));
			Assert.False(view.AnimationIsRunning(textColorToAnimationName), "A canceled call must not leave an animation running.");
			Assert.Equal(Colors.Blue, view.TextColor);
		});
	}

	async Task AssertTextColorToAnimatesAsync(View view, Label childLabel, Func<Color> getTextColor, Func<Color, Task<bool>> animateTextColor)
	{
		SetupBuilder();

		await AttachAndRun(view, async _ =>
		{
			await view.OnLoadedAsync();
			Assert.Equal(Colors.Blue, getTextColor());

			// When Android's animations are off, MAUI ends the animation at once.
			Assert.True(await animateTextColor(Colors.Red));

			Assert.Equal(Colors.Red, getTextColor());
			Assert.Equal(Colors.Red, childLabel.TextColor);
			Assert.False(view.AnimationIsRunning(textColorToAnimationName), "The animation must be finished.");
		});
	}
}

public partial class TextViewContainer
{
	public sealed partial class NestedTextView : ContentView, ITextStyle
	{
		public NestedTextView()
		{
			ChildLabel.TextColor = TextColor;
			Content = ChildLabel;
		}

		[BindableProperty(PropertyChangedMethodName = nameof(OnTextColorPropertyChanged))]
		public partial Color TextColor { get; set; } = Colors.Blue;

		public Label ChildLabel { get; } = new();

		static void OnTextColorPropertyChanged(BindableObject bindable, object oldValue, object newValue) => ((NestedTextView)bindable).ChildLabel.TextColor = (Color)newValue;

		public Microsoft.Maui.Font Font => default;

		public double CharacterSpacing => 0;
	}

	internal partial class Middle
	{
		public sealed partial class DeeplyNestedTextView : ContentView, ITextStyle
		{
			public DeeplyNestedTextView()
			{
				ChildLabel.TextColor = TextColor;
				Content = ChildLabel;
			}

			[BindableProperty(PropertyChangedMethodName = nameof(OnTextColorPropertyChanged))]
			public partial Color TextColor { get; set; } = Colors.Blue;

			public Label ChildLabel { get; } = new();

			static void OnTextColorPropertyChanged(BindableObject bindable, object oldValue, object newValue) => ((DeeplyNestedTextView)bindable).ChildLabel.TextColor = (Color)newValue;

			public Microsoft.Maui.Font Font => default;

			public double CharacterSpacing => 0;
		}
	}
}

public partial class GenericTextViewContainer<T>
{
	public sealed partial class NestedTextView : ContentView, ITextStyle
	{
		public NestedTextView()
		{
			ChildLabel.TextColor = TextColor;
			Content = ChildLabel;
		}

		[BindableProperty(PropertyChangedMethodName = nameof(OnTextColorPropertyChanged))]
		public partial Color TextColor { get; set; } = Colors.Blue;

		public Label ChildLabel { get; } = new();

		static void OnTextColorPropertyChanged(BindableObject bindable, object oldValue, object newValue) => ((NestedTextView)bindable).ChildLabel.TextColor = (Color)newValue;

		public Microsoft.Maui.Font Font => default;

		public double CharacterSpacing => 0;
	}
}