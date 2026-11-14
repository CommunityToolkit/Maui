namespace CommunityToolkit.Maui.Media;

static class SilenceTimeoutMilliseconds
{
	// Google's recognizer reads the silence-length extras as Integer; a Long makes Bundle ignore them (logcat: "expected Integer but value was a java.lang.Long").
	public static int From(TimeSpan timeout)
	{
		var milliseconds = timeout.TotalMilliseconds;

		if (milliseconds >= int.MaxValue)
		{
			return int.MaxValue;
		}

		return Math.Max(1, (int)milliseconds);
	}
}