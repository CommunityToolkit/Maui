using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.DeviceTests;

// Derives from the plain VisualElement, so that giving it any handler does not set up gesture managers or other platform plumbing that
// expects a real platform view. It is a top-level type, not a nested one, because the toolkit's TextColorTo source generator generates
// code for every ITextStyle view and cannot reference nested types.
sealed class TextInputStub : VisualElement, ITextInput
{
	public string Text { get; set; } = string.Empty;

	public bool IsTextPredictionEnabled => false;

	public bool IsSpellCheckEnabled => false;

	public bool IsReadOnly => false;

	public Keyboard Keyboard => Keyboard.Default;

	public int MaxLength => int.MaxValue;

	public int CursorPosition { get; set; }

	public int SelectionLength { get; set; }

	public string Placeholder => string.Empty;

	public Color PlaceholderColor { get; set; } = Colors.Gray;

	public Color TextColor { get; set; } = Colors.Black;

	public Microsoft.Maui.Font Font => default;

	public double CharacterSpacing => 0;
}