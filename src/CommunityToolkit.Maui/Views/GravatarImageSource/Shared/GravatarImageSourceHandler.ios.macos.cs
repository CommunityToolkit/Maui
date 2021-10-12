using System;
using Foundation;

namespace CommunityToolkit.Maui.Views
{
	public partial class GravatarImageSourceHandler
	{
		static string GetCacheDirectory()
		{
			var dirs = NSSearchPath.GetDirectories(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User);
			if (dirs == null || dirs.Length == 0)
				throw new NotSupportedException();

			return dirs[0];
		}
	}
}