using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Application = Android.App.Application;

namespace CommunityToolkit.Maui.Views
{
	public partial class GravatarImageSourceHandler : IImageSourceHandler
	{
		public async Task<Bitmap?> LoadImageAsync(ImageSource imagesource, Context context, CancellationToken cancelationToken = default)
		{
			var fileInfo = await LoadInternal(imagesource, 1, Application.Context.CacheDir?.AbsolutePath ?? throw new NullReferenceException());

			Bitmap? bitmap = null;
			try
			{
				await semaphore.WaitAsync(cancelationToken);

				if (fileInfo?.Exists ?? false)
					bitmap = await BitmapFactory.DecodeFileAsync(fileInfo.FullName);
			}
			finally
			{
				semaphore.Release();
			}

			return bitmap;
		}
	}
}