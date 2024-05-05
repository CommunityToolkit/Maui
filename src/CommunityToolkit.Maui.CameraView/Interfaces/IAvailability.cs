namespace CommunityToolkit.Maui.Core;

public interface IAvailability
{
	bool IsAvailable { get; set; }
	bool IsBusy { get; set; }
}