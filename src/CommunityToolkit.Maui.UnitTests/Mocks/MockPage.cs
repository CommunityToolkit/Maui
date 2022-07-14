using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class MockPage : Page
{
	public MockPage(MockPageViewModel viewModel)
	{
		BindingContext = viewModel;
	}
}
