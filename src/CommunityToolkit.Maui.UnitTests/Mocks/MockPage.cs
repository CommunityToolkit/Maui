namespace CommunityToolkit.Maui.UnitTests.Mocks;

class MockPage : Page
{
	public MockPage(MockPageViewModel viewModel)
	{
		BindingContext = viewModel;
	}
}