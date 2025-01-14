using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupTests : BaseHandlerTest
{
	public PopupTests()
	{
		Assert.IsType<Popup>(new MockPopup(), exactMatch: false);
	}

	[Fact]
	public void NullColorThrowsArgumentNullException()
	{
		var popupViewModel = new PopupViewModel();
		var popupWithBinding = new PopupContainer
		{
			BindingContext = popupViewModel
		};
		popupWithBinding.SetBinding(PopupContainer.BackgroundColorProperty, nameof(PopupViewModel.Color));

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new PopupContainer { BackgroundColor = null });
		Assert.Throws<ArgumentNullException>(() => new PopupContainer().BackgroundColor = null);
		Assert.Throws<ArgumentNullException>(() => popupViewModel.Color = null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}

public class MockPopup : Popup
{
}

class PopupViewModel : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	public Color? Color
	{
		get;
		set
		{
			if (!Equals(value, field))
			{
				field = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
			}
		}
	} = new();
}