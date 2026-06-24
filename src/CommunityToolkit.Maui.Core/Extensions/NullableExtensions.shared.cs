namespace CommunityToolkit.Maui.Core.Extensions;

static class NullableExtensions
{
	public static bool IsNullable(this Type type)
	{
		if (!type.IsValueType)
		{
			return true; // ref-type
		}

		if (Nullable.GetUnderlyingType(type) is not null)
		{
			return true; // Nullable<T>
		}

		return false; // value-type
	}
}