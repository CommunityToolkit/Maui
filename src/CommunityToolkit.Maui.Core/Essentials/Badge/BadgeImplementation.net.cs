namespace CommunityToolkit.Maui.ApplicationModel;

/// <inheritdoc />
public class BadgeImplementation : IBadge
{
	/// <inheritdoc />
	public void SetCount(uint count)
	{
		throw new NotSupportedException($"{nameof(IBadge)} requires a platform-specific implementation of .NET");
	}
}