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

	public double GetOriginalValue(BindableProperty property) => originalValues[property];

	public Action<double> GetTranslationSetter(BindableProperty property) => translationComposers[property].Write;

	public double GetValueToRestore(BindableProperty property) =>
		translationComposers.TryGetValue(property, out var composer) ? composer.GetValueToRestore() : originalValues[property];

	public void StopComposingTranslations()
	{
		foreach (var composer in translationComposers.Values)
		{
			composer.Stop();
		}
	}

	// A .NET MAUI TranslateToAsync keeps running under an attention seeker and writes the translation as an absolute value too, so the keyframes
	// ride on top of the value the other writer left: the keyframes' offset from the original value is added to it.
	sealed class TranslationComposer
	{
		readonly VisualElement view;
		readonly BindableProperty property;
		readonly double originalValue;

		// An equal write raises no PropertyChanged, but BindableObject still stores the new box, which tells another writer's equal write apart from none.
		// A .NET MAUI that boxed on every read would make every read look like another writer's, so it falls back to comparing values
		readonly bool canSeeAnEqualWrite;

		double baseValue;
		double keyframeValue;
		object? lastWrittenValue;
		bool isWriting;

		public TranslationComposer(VisualElement view, BindableProperty property, double originalValue)
		{
			this.view = view;
			this.property = property;
			this.originalValue = originalValue;
			canSeeAnEqualWrite = ReferenceEquals(view.GetValue(property), view.GetValue(property));
		}

		public void Write(double keyframeValue)
		{
			if (lastWrittenValue is null)
			{
				// A writer whose animation started after this run ticks after it in every frame, so its write would otherwise be the one the frame shows
				view.BatchCommitted += HandleBatchCommitted;
			}

			if (WasWrittenByAnotherWriter())
			{
				baseValue = (double)view.GetValue(property);
			}

			this.keyframeValue = keyframeValue;
			WriteComposedValue();
		}

		public void Stop() => view.BatchCommitted -= HandleBatchCommitted;

		public double GetValueToRestore() => WasWrittenByAnotherWriter() ? (double)view.GetValue(property) : baseValue;

		void HandleBatchCommitted(object? sender, EventArgs e)
		{
			// A PropertyChanged handler that batches the view raises this inside this composer's own write
			if (!isWriting && WasWrittenByAnotherWriter())
			{
				baseValue = (double)view.GetValue(property);
				WriteComposedValue();
			}
		}

		bool WasWrittenByAnotherWriter()
		{
			var currentValue = view.GetValue(property);

			return canSeeAnEqualWrite ? !ReferenceEquals(currentValue, lastWrittenValue) : !Equals(currentValue, lastWrittenValue);
		}

		void WriteComposedValue()
		{
			isWriting = true;
			try
			{
				// Adding 0 when nothing else writes the property keeps every frame exactly the keyframe's value
				view.SetValue(property, keyframeValue + (baseValue - originalValue));
			}
			finally
			{
				isWriting = false;

				// Read back even when a PropertyChanged handler throws or answers this write, so neither looks like another writer's write next time
				lastWrittenValue = view.GetValue(property);
			}
		}
	}
}