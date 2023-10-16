using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class CustomSizeAndPositionPopupViewModel : BaseViewModel
{
	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(ExecuteShowButtonCommand))]
	double height = 100, width = 100;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(ExecuteShowButtonCommand))]
	bool isStartHorizontalOptionSelected = true, isCenterHorizontalOptionSelected, isEndHorizontalOptionSelected, isFillHorizontalOptionSelected,
		isStartVerticalOptionSelected = true, isCenterVerticalOptionSelected, isEndVerticalOptionSelected, isFillVerticalOptionSelected;

	[RelayCommand(CanExecute = nameof(CanShowButtonExecute))]
	public Task ExecuteShowButton()
	{
		Microsoft.Maui.Primitives.LayoutAlignment? verticalOptions = null, horizontalOptions = null;

		if (IsStartVerticalOptionSelected)
		{
			verticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Start;
		}
		if (IsCenterVerticalOptionSelected)
		{
			verticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Center;
		}
		if (IsEndVerticalOptionSelected)
		{
			verticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.End;
		}
		if (IsFillVerticalOptionSelected)
		{
			verticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Fill;
		}

		ArgumentNullException.ThrowIfNull(verticalOptions);

		if (IsStartHorizontalOptionSelected)
		{
			horizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Start;
		}
		if (IsCenterHorizontalOptionSelected)
		{
			horizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Center;
		}
		if (IsEndHorizontalOptionSelected)
		{
			horizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.End;
		}
		if (IsFillHorizontalOptionSelected)
		{
			horizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Fill;
		}

		ArgumentNullException.ThrowIfNull(horizontalOptions);

		var popup = new RedBlueBoxPopup
		{
			Size = new Size(Width, Height),
			VerticalOptions = verticalOptions.Value,
			HorizontalOptions = horizontalOptions.Value
		};

		return Shell.Current.ShowPopupAsync(popup);
	}

	// Ensure at least one Horizontal Option is selected, one Vertical Option is selected, Height > 0, and Width > 0
	bool CanShowButtonExecute() => (IsStartHorizontalOptionSelected || IsCenterHorizontalOptionSelected || IsEndHorizontalOptionSelected || IsFillHorizontalOptionSelected)
		&& (IsStartVerticalOptionSelected || IsCenterVerticalOptionSelected || IsEndVerticalOptionSelected || IsFillVerticalOptionSelected)
		&& Height > 0
		&& Width > 0;
}