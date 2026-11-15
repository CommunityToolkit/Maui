namespace CommunityToolkit.Maui.Animations;

sealed class KeyframeTarget
{
	readonly IReadOnlyDictionary<BindableProperty, double> originalValues;
	readonly Dictionary<BindableProperty, TranslationComposer> translationComposers = [];

	public KeyframeTarget(VisualElement view, IReadOnlyList<BindableProperty> animatedProperties, IReadOnlyDictionary<BindableProperty, double> originalValues)
	{
		View = view;
		this.originalValues = originalValues;

		foreach (var property in animatedProperties)
		{
			if (property == VisualElement.TranslationXProperty || property == VisualElement.TranslationYProperty)
			{
				translationComposers[property] = new TranslationComposer(view, property, originalValues[property]);
			}
		}
	}

	public VisualElement View { get; }

	public double OriginalValueOf(BindableProperty property) => originalValues[property];

	public Action<double> SetterFor(BindableProperty property) =>
		translationComposers.TryGetValue(property, out var composer) ? composer.Write : value => View.SetValue(property, value);

	public double GetValueToRestore(BindableProperty property) =>
		translationComposers.TryGetValue(property, out var composer) ? composer.GetValueToRestore() : originalValues[property];

	// A .NET MAUI TranslateToAsync keeps running under an attention seeker and writes the translation as an absolute value too, so the keyframes
	// ride on top of the value the other writer left: the keyframes' offset from the original value is added to it.
	sealed class TranslationComposer(VisualElement view, BindableProperty property, double originalValue)
	{
		double baseValue;
		double lastWrittenValue;
		bool hasWritten;

		public void Write(double keyframeValue)
		{
			var currentValue = (double)view.GetValue(property);

			if (!hasWritten || currentValue != lastWrittenValue)
			{
				baseValue = currentValue;
			}

			// Adding 0 when nothing else writes the property keeps every frame exactly the keyframe's value
			lastWrittenValue = keyframeValue + (baseValue - originalValue);
			hasWritten = true;
			view.SetValue(property, lastWrittenValue);
		}

		public double GetValueToRestore()
		{
			var currentValue = (double)view.GetValue(property);

			return hasWritten && currentValue == lastWrittenValue ? baseValue : currentValue;
		}
	}
}