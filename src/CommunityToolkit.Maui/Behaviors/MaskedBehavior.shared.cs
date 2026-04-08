using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The MaskedBehavior is a behavior that allows the user to define an input mask for data entry. Adding this behavior to an <see cref="InputView"/> (i.e. <see cref="Entry"/>) control will force the user to only input values matching a given mask. Examples of its usage include input of a credit card number or a phone number.
/// </summary>
public partial class MaskedBehavior : BaseBehavior<InputView>, IDisposable
{
	readonly SemaphoreSlim applyMaskSemaphoreSlim = new(1, 1);

	bool isDisposed;

	IReadOnlyDictionary<int, char>? maskPositions;

	/// <summary>
	/// Finalizer
	/// </summary>
	~MaskedBehavior() => Dispose(false);

	/// <summary>
	/// The mask that the input value needs to match. This is a bindable property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnMaskPropertyChanged))]
	public partial string? Mask { get; set; }

	/// <summary>
	/// Gets or sets which character in the <see cref="Mask"/> property that will be visible and entered by a user. Defaults to 'X'. This is a bindable property.
	/// <br/>
	/// By default, the 'X' character will be unmasked therefore a <see cref="Mask"/> of "XX XX XX" would display "12 34 56".
	/// If you wish to include 'X' in your <see cref="Mask"/> then you could set this <see cref="UnmaskedCharacter"/> to something else
	/// e.g. '0' and then use a <see cref="Mask"/> of "00X00X00" which would then display "12X34X56".
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(UnmaskedCharacterValueCreator), PropertyChangedMethodName = nameof(OnUnmaskedCharacterPropertyChanged))]
	public partial char UnmaskedCharacter { get; set; }

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc/>
	protected virtual void Dispose(bool disposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (disposing)
		{
			applyMaskSemaphoreSlim.Dispose();
		}

		isDisposed = true;
	}

	/// <inheritdoc />
	protected override async void OnViewPropertyChanged(InputView sender, PropertyChangedEventArgs e)
	{
		base.OnViewPropertyChanged(sender, e);

		if (e.PropertyName == InputView.TextProperty.PropertyName)
		{
			await OnTextPropertyChanged(CancellationToken.None);
		}
	}

	static object UnmaskedCharacterValueCreator(BindableObject bindable) => 'X';

	static void OnMaskPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var mask = (string?)newValue;
		var maskedBehavior = (MaskedBehavior)bindable;

		maskedBehavior.SetMaskPositions(mask);
	}

	static async void OnUnmaskedCharacterPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var maskedBehavior = (MaskedBehavior)bindable;
		await maskedBehavior.OnMaskChanged(maskedBehavior.Mask, CancellationToken.None).ConfigureAwait(false);
	}

	Task OnTextPropertyChanged(CancellationToken token)
	{
		// Android does not play well when we update the Text inside the TextChanged event. 
		// Therefore, if we dispatch the mechanism of updating the Text property it solves the issue of the caret position being updated incorrectly.
		// https://github.com/CommunityToolkit/Maui/issues/460
		return View?.Dispatcher.DispatchAsync(() => ApplyMask(View?.Text, token)) ?? Task.CompletedTask;
	}

	void SetMaskPositions(in string? mask)
	{
		if (string.IsNullOrEmpty(mask))
		{
			maskPositions = null;
			return;
		}

		var list = new Dictionary<int, char>();

		for (var i = 0; i < mask.Length; i++)
		{
			if (mask[i] != UnmaskedCharacter)
			{
				list.Add(i, mask[i]);
			}
		}

		maskPositions = list;
	}

	async ValueTask OnMaskChanged(string? mask, CancellationToken token)
	{
		if (string.IsNullOrEmpty(mask))
		{
			maskPositions = null;
			return;
		}

		var originalText = RemoveMask(View?.Text);

		SetMaskPositions(mask);

		await ApplyMask(originalText, token);
	}

	[return: NotNullIfNotNull(nameof(text))]
	string? RemoveMask(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}

		var maskChars = maskPositions?
			.Select(c => c.Value)
			.Distinct()
			.ToArray();

		return string.Join(string.Empty, text.Split(maskChars));
	}

	async Task ApplyMask(string? text, CancellationToken token)
	{
		await applyMaskSemaphoreSlim.WaitAsync(token);

		try
		{
			if (!string.IsNullOrWhiteSpace(text) && maskPositions is not null)
			{
				if (Mask is not null && text.Length > Mask.Length)
				{
					text = text.Remove(text.Length - 1);
				}

				text = RemoveMask(text);
				foreach (var position in maskPositions)
				{
					if (text.Length < position.Key + 1)
					{
						continue;
					}

					var value = position.Value.ToString();

					// !important - If user types in masked value, don't add masked value
					if (text.Substring(position.Key, 1) != value)
					{
						text = text.Insert(position.Key, value);
					}
				}
			}

			if (View is not null)
			{
				View.Text = text;
			}
		}
		finally
		{
			applyMaskSemaphoreSlim.Release();
		}
	}
}