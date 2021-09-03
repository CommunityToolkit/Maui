namespace CommunityToolkit.Maui.UnitTests.Mocks
{
	public class MockItem
	{
		public string? Title { get; set; }

		public bool Completed { get; set; }

		public override string ToString() => Completed ?
			$"{Title} is completed" : $"{Title} has yet to be completed";
	}
}