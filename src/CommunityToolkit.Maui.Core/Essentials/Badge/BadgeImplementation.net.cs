namespace CommunityToolkit.Maui.ApplicationModel;

/// <inheritdoc />
public class BadgeImplementation : IBadge
{
	/// <inheritdoc />
	public void SetCount(int count)
	{
		throw new NotSupportedException($"{nameof(IBadge)} requires a platform-specific implementation of .NET");
	}

	/// <inheritdoc />
	public int GetCount()
	{
		throw new NotSupportedException($"{nameof(IBadge)} requires a platform-specific implementation of .NET");
	}
}