namespace CommunityToolkit.Maui.UnitTests.Mocks;

class MockPage : ContentPage
{
	public MockPage(MockPageViewModel viewModel)
	{
		BindingContext = viewModel;
	}
}