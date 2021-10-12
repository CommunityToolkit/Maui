using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility.Platform.iOS;
using UIKit;

namespace CommunityToolkit.Maui.Views
{
	public partial class GravatarImageSourceHandler : IImageSourceHandler
	{
		public async Task<UIImage?> LoadImageAsync(ImageSource imagesource, CancellationToken cancelationToken = default, float scale = 1)
		{
			var fileInfo = await LoadInternal(imagesource, scale, GetCacheDirectory());

			UIImage? image = null;
			try
			{
				await semaphore.WaitAsync(cancelationToken);

				if (fileInfo?.Exists ?? false)
					image = UIImage.FromFile(fileInfo.FullName);
			}
			finally
			{
				semaphore.Release();
			}

			return image;
		}
	}
}