using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

// The states are driven through the behavior's internal HandleTouch, HandleHover and HandleUserInteraction, which every platform implementation
// calls, so these tests run on every platform including Windows. TouchBehaviorInputTests covers the platform implementations.
[Category(TestCategory.TouchBehavior)]
[Collection(TestCollections.DisableParallelization)]
public class GestureManagerEffectTests : TouchBehaviorTestBase
{
	enum EffectState
	{
		Default,
		Hovered,
		Pressed
	}

	sealed record EffectCase(string Name, double Neutral, double DefaultValue, double HoveredValue, double PressedValue, Action<TouchBehavior, EffectState, double?> Set, Func<IPlatformViewHandler, double> ReadNative, double OwnValue, Action<VisualElement, double> SetOnElement, double Tolerance = 0.02);

	// Android stores a translation in whole pixels (MAUI rounds it), so a value in device independent units is only exact to a pixel.
	const double translationTolerance = 0.5;

	static readonly Dictionary<string, EffectCase> effectCases = new()
	{
		["Opacity"] = new("Opacity", 1, 0.8, 0.6, 0.4, static (behavior, state, value) =>
		{
			switch (state)
			{
				case EffectState.Default: behavior.DefaultOpacity = value; break;
				case EffectState.Hovered: behavior.HoveredOpacity = value; break;
				default: behavior.PressedOpacity = value; break;
			}
		}, GetNativeOpacity, 0.7, static (element, value) => element.Opacity = value),
		["Scale"] = new("Scale", 1, 1.2, 1.4, 1.6, static (behavior, state, value) =>
		{
			switch (state)
			{
				case EffectState.Default: behavior.DefaultScale = value; break;
				case EffectState.Hovered: behavior.HoveredScale = value; break;
				default: behavior.PressedScale = value; break;
			}
		}, GetNativeScale, 1.3, static (element, value) => element.Scale = value),
		["TranslationX"] = new("TranslationX", 0, 10, 20, 30, static (behavior, state, value) =>
		{
			switch (state)
			{
				case EffectState.Default: behavior.DefaultTranslationX = value; break;
				case EffectState.Hovered: behavior.HoveredTranslationX = value; break;
				default: behavior.PressedTranslationX = value; break;
			}
		}, GetNativeTranslationX, 7, static (element, value) => element.TranslationX = value, translationTolerance),
		["TranslationY"] = new("TranslationY", 0, 12, 24, 36, static (behavior, state, value) =>
		{
			switch (state)
			{
				case EffectState.Default: behavior.DefaultTranslationY = value; break;
				case EffectState.Hovered: behavior.HoveredTranslationY = value; break;
				default: behavior.PressedTranslationY = value; break;
			}
		}, GetNativeTranslationY, 9, static (element, value) => element.TranslationY = value, translationTolerance),
		["Rotation"] = new("Rotation", 0, 15, 30, 45, static (behavior, state, value) =>
		{
			switch (state)
			{
				case EffectState.Default: behavior.DefaultRotation = value; break;
				case EffectState.Hovered: behavior.HoveredRotation = value; break;
				default: behavior.PressedRotation = value; break;
			}
		}, GetNativeRotation, 11, static (element, value) => element.Rotation = value, 0.1),
		["RotationX"] = new("RotationX", 0, 10, 20, 30, static (behavior, state, value) =>
		{
			switch (state)
			{
				case EffectState.Default: behavior.DefaultRotationX = value; break;
				case EffectState.Hovered: behavior.HoveredRotationX = value; break;
				default: behavior.PressedRotationX = value; break;
			}
		}, GetNativeRotationX, 13, static (element, value) => element.RotationX = value, 0.1),
		["RotationY"] = new("RotationY", 0, 10, 20, 30, static (behavior, state, value) =>
		{
			switch (state)
			{
				case EffectState.Default: behavior.DefaultRotationY = value; break;
				case EffectState.Hovered: behavior.HoveredRotationY = value; break;
				default: behavior.PressedRotationY = value; break;
			}
		}, GetNativeRotationY, 17, static (element, value) => element.RotationY = value, 0.1),
	};

	static Task AssertEffectAsync(EffectCase effect, IPlatformViewHandler handler, double expected, string step) =>
		AssertNativeValueAsync(() => effect.ReadNative(handler), expected, $"{effect.Name} {step}", effect.Tolerance);

	[Theory]
	[InlineData("Opacity")]
	[InlineData("Scale")]
	[InlineData("TranslationX")]
	[InlineData("TranslationY")]
	[InlineData("Rotation")]
	[InlineData("RotationX")]
	[InlineData("RotationY")]
	public async Task EachStateAppliesItsOwnValueToThePlatformView(string name)
	{
		SetupBuilder();
		var effect = effectCases[name];
		var behavior = new TouchBehavior();
		effect.Set(behavior, EffectState.Default, effect.DefaultValue);
		effect.Set(behavior, EffectState.Hovered, effect.HoveredValue);
		effect.Set(behavior, EffectState.Pressed, effect.PressedValue);

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await AssertEffectAsync(effect, handler, effect.DefaultValue, "after attach (default state)");

			behavior.HandleHover(HoverStatus.Entered);
			await AssertEffectAsync(effect, handler, effect.HoveredValue, "while hovered");

			SimulatePress(behavior);
			await AssertEffectAsync(effect, handler, effect.PressedValue, "while pressed (pressed wins over hovered)");

			SimulateRelease(behavior);
			await AssertEffectAsync(effect, handler, effect.HoveredValue, "after release while still hovered");

			behavior.HandleHover(HoverStatus.Exited);
			await AssertEffectAsync(effect, handler, effect.DefaultValue, "after the hover ended");
		});
	}

	[Theory]
	[InlineData("Opacity")]
	[InlineData("Scale")]
	[InlineData("TranslationX")]
	[InlineData("TranslationY")]
	[InlineData("Rotation")]
	[InlineData("RotationX")]
	[InlineData("RotationY")]
	public async Task HoveredStateFallsBackToTheDefaultValue(string name)
	{
		SetupBuilder();
		var effect = effectCases[name];
		var behavior = new TouchBehavior();
		effect.Set(behavior, EffectState.Default, effect.DefaultValue);
		effect.Set(behavior, EffectState.Pressed, effect.PressedValue);

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			behavior.HandleHover(HoverStatus.Entered);

			await AssertEffectAsync(effect, handler, effect.DefaultValue, "while hovered without a hovered value");
		});
	}

	[Theory]
	[InlineData("Opacity")]
	[InlineData("Scale")]
	[InlineData("TranslationX")]
	[InlineData("TranslationY")]
	[InlineData("Rotation")]
	[InlineData("RotationX")]
	[InlineData("RotationY")]
	public async Task PressedStateFallsBackToTheDefaultValue(string name)
	{
		SetupBuilder();
		var effect = effectCases[name];
		var behavior = new TouchBehavior();
		effect.Set(behavior, EffectState.Default, effect.DefaultValue);
		effect.Set(behavior, EffectState.Hovered, effect.HoveredValue);

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			SimulatePress(behavior);

			await AssertEffectAsync(effect, handler, effect.DefaultValue, "while pressed without a pressed value");
		});
	}

	[Theory]
	[InlineData("Opacity")]
	[InlineData("Scale")]
	[InlineData("TranslationX")]
	[InlineData("TranslationY")]
	[InlineData("Rotation")]
	[InlineData("RotationX")]
	[InlineData("RotationY")]
	public async Task OnlyAPressedValueIsAppliedWhilePressed(string name)
	{
		SetupBuilder();
		var effect = effectCases[name];
		var behavior = new TouchBehavior();
		effect.Set(behavior, EffectState.Pressed, effect.PressedValue);

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await AssertEffectAsync(effect, handler, effect.Neutral, "before the press");

			SimulatePress(behavior);
			await AssertEffectAsync(effect, handler, effect.PressedValue, "while pressed");

			SimulateRelease(behavior);
			await AssertEffectAsync(effect, handler, effect.Neutral, "after release");
		});
	}

	[Theory]
	[InlineData("Opacity")]
	[InlineData("Scale")]
	[InlineData("TranslationX")]
	[InlineData("TranslationY")]
	[InlineData("Rotation")]
	[InlineData("RotationX")]
	[InlineData("RotationY")]
	public async Task OnlyAHoveredValueIsAppliedWhileHovered(string name)
	{
		SetupBuilder();
		var effect = effectCases[name];
		var behavior = new TouchBehavior();
		effect.Set(behavior, EffectState.Hovered, effect.HoveredValue);

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await AssertEffectAsync(effect, handler, effect.Neutral, "before the hover");

			behavior.HandleHover(HoverStatus.Entered);
			await AssertEffectAsync(effect, handler, effect.HoveredValue, "while hovered");

			behavior.HandleHover(HoverStatus.Exited);
			await AssertEffectAsync(effect, handler, effect.Neutral, "after the hover ended");
		});
	}

	[Theory]
	[InlineData("Opacity")]
	[InlineData("Scale")]
	[InlineData("TranslationX")]
	[InlineData("TranslationY")]
	[InlineData("Rotation")]
	[InlineData("RotationX")]
	[InlineData("RotationY")]
	public async Task NothingConfiguredLeavesTheElementsOwnValueAlone(string name)
	{
		SetupBuilder();
		var effect = effectCases[name];
		var host = CreateHost();
		effect.SetOnElement(host, effect.OwnValue);
		var behavior = new TouchBehavior();

		await RunAttachedAsync(host, behavior, async handler =>
		{
			await AssertEffectAsync(effect, handler, effect.OwnValue, "after attach (the element's own value)");

			behavior.HandleHover(HoverStatus.Entered);
			SimulatePress(behavior);
			await AssertNativeValueStaysAsync(() => effect.ReadNative(handler), effect.OwnValue, $"{effect.Name} while hovered and pressed", effect.Tolerance);

			SimulateRelease(behavior);
			behavior.HandleHover(HoverStatus.Exited);
			await AssertNativeValueStaysAsync(() => effect.ReadNative(handler), effect.OwnValue, $"{effect.Name} after the touch", effect.Tolerance);
		});
	}

	[Theory]
	[InlineData("Opacity")]
	[InlineData("Scale")]
	[InlineData("TranslationX")]
	[InlineData("TranslationY")]
	[InlineData("Rotation")]
	[InlineData("RotationX")]
	[InlineData("RotationY")]
	public async Task StateEffectsReachTheirTargetWhenADurationIsConfigured(string name)
	{
		SetupBuilder();
		var effect = effectCases[name];
		var behavior = new TouchBehavior { PressedAnimationDuration = 300, DefaultAnimationDuration = 0 };
		effect.Set(behavior, EffectState.Pressed, effect.PressedValue);

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			await EnableAnimationsAsync(handler);

			SimulatePress(behavior);
			await AssertNativeValueAsync(() => effect.ReadNative(handler), effect.PressedValue, $"{effect.Name} after the pressed animation", effect.Tolerance, 6000);

			SimulateRelease(behavior);
			await AssertEffectAsync(effect, handler, effect.Neutral, "right after release");
		});
	}

	[Fact]
	public async Task EffectsAreNotAppliedWhileTheBehaviorIsDisabled()
	{
		SetupBuilder();
		var behavior = new TouchBehavior { PressedOpacity = 0.4, PressedScale = 1.5, PressedRotation = 30 };

		await RunAttachedAsync(CreateHost(), behavior, async handler =>
		{
			behavior.IsEnabled = false;

			// Set directly, because the disabled touch handling would ignore a press before the animation task is reached.
			behavior.CurrentTouchState = TouchState.Pressed;
			await SettleAsync();

			Assert.Equal(1, GetNativeOpacity(handler), 2);
			Assert.Equal(1, GetNativeScale(handler), 2);
			Assert.Equal(0, GetNativeRotation(handler), 1);

			behavior.CurrentTouchState = TouchState.Default;
			await SettleAsync();
		});
	}

	// Catches: ForceUpdateState's Element guard missing. The view stays alone anyway (ChangeStateAsync and RunAnimationTask check the
	// element too), but ChangeStateAsync replaces the animation token source that the detach released.
	[Fact]
	public async Task EffectsStopWhenTheBehaviorIsRemoved()
	{
		SetupBuilder();
		var behavior = new TouchBehavior { PressedOpacity = 0.4 };
		var host = CreateHost();

		await RunAttachedAsync(host, behavior, async handler =>
		{
			var manager = GetGestureManager(behavior);
			host.Behaviors.Remove(behavior);
			Assert.Null(behavior.Element);
			await WaitUntilAsync(() => GetAnimationTokenSource(manager) is null, "The detach must release the animation token source of the attach's state update.");

			behavior.CurrentTouchState = TouchState.Pressed;
			await SettleAsync();

			Assert.Equal(1, GetNativeOpacity(handler), 2);
			Assert.Null(GetAnimationTokenSource(manager));

			behavior.CurrentTouchState = TouchState.Default;
		});
	}
}