using System.ComponentModel;
using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class RotateAnimationTests : ToAnimationTestBase<RotateAnimation>
{
	const double originalRotation = 10;
	const double rotation = 180;

	protected override IReadOnlyList<(BindableProperty Property, double Value)> OriginalValues { get; } = [(VisualElement.RotationProperty, originalRotation)];

	protected override BindableProperty PeakProperty => VisualElement.RotationProperty;

	protected override double PeakValue => originalRotation + rotation;

	protected override RotateAnimation CreateAnimation() => new() { Rotation = rotation, Length = runLength };

	[Fact]
	public async Task DefaultRotationTurnsATiltedViewAFullTurnAndEndsAtItsTilt()
	{
		SetupBuilder();

		var view = CreateAnimatedView(OriginalValues);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var rotations = new List<double>();
			view.PropertyChanged += RecordRotation;
			try
			{
				await new RotateAnimation { Length = runLength }.Animate(view).WaitAsync(waitTimeout);
			}
			finally
			{
				view.PropertyChanged -= RecordRotation;
			}

			// The turn ends a whole turn from the tilt, which looks the same, so the jump back to the tilt is invisible
			Assert.True(rotations.Count >= 2, $"The view's rotation changed {rotations.Count} time(s).");
			Assert.True(Math.Abs(rotations[^2] - (originalRotation + 360)) <= 1e-6, $"The turn ended at {rotations[^2]}, expected {originalRotation + 360}.");
			Assert.Equal(originalRotation, rotations[^1]);
			AssertAtOriginalValues(handler, view, OriginalValues, nativeStateAtRest, "after a full turn");

			void RecordRotation(object? sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName == nameof(VisualElement.Rotation))
				{
					rotations.Add(view.Rotation);
				}
			}
		});
	}
}
