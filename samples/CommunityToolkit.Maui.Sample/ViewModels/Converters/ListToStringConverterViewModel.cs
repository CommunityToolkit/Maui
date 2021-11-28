using System.Collections.Generic;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ListToStringConverterViewModel : BaseViewModel
{
	public IReadOnlyList<string> DummyItemSource { get; } = new[]
	{
			"Item 0",
			"Item 1",
			"Item 2",
			"Item 3",
			"Item 4",
			"Item 5",
	};
}